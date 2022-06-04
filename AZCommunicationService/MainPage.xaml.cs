using Azure.Communication.Calling;
using Azure.Communication.Identity;
using Azure.WinRT.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AZCommunicationService
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

       // private CommunicationIdentityClient communicationIdentityClient;
        private Call call_;
        private CallClient call_client;
        private CallAgent call_agent;
        private DeviceManager deviceManager;
        private LocalVideoStream[] localVideoStream;
      //  private Authenticator authenticator;
        private string userToken;

        public MainPage()
        {
            this.InitializeComponent();
            //InitCommunicationIdentityClient();
            InitCallClientAndDeviceManager();
           // authenticator = new Authenticator();
        }
        private async void InitCallClientAndDeviceManager()
        {
            call_client = new CallClient();
            deviceManager = await call_client.GetDeviceManager();
        }

        //private void InitCommunicationIdentityClient()
        //{
        //    communicationIdentityClient = new CommunicationIdentityClient(communicationServicesConnectionString);
        //}

        private async void JoinButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (!await ValidateInput())
            {
                return;
            }

            await CreateCallAgent();

            await CreateLocalVideoStream();

            await JoinTeamsMeeting();
        }

        private async Task CreateCallAgent()
        {
            try
            {
                //if (string.IsNullOrEmpty(userToken))
                //{
                //    var token = await authenticator.AcquireTokenAsync();

                //    // https://docs.microsoft.com/en-us/azure/communication-services/quickstarts/manage-teams-identity?pivots=programming-language-csharp
                //    var accessToken = await communicationIdentityClient.GetTokenForTeamsUserAsync(token);
                //    userToken = accessToken.Value.Token;
                //}

                if (call_agent is null)
                {
                    userToken = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjEwNCIsIng1dCI6IlJDM0NPdTV6UENIWlVKaVBlclM0SUl4Szh3ZyIsInR5cCI6IkpXVCJ9.eyJza3lwZWlkIjoiYWNzOjljMzU2ZjQ2LTc4YjMtNDg4OC1iMWEzLWFlMDM0NTkxYmIyMl8wMDAwMDAwZi1hNzMwLTkyNDctNTUwNC01NzQ4MjIwMGVhMjgiLCJzY3AiOjE3OTIsImNzaSI6IjE2NDUwOTUwODciLCJleHAiOjE2NDUxODE0ODcsImFjc1Njb3BlIjoidm9pcCIsInJlc291cmNlSWQiOiI5YzM1NmY0Ni03OGIzLTQ4ODgtYjFhMy1hZTAzNDU5MWJiMjIiLCJpYXQiOjE2NDUwOTUwODd9.Tu4KXXWwNa_Ucq7MDPDqvj1-G3kvx2NU63ORZ6th6lEm0X5j-YO9JQbv5tnlcJx4-BGcbwCUdtiqzQ8SpPgQUOL6S4IpiIvGHxcdtYuGcRHhoR_vyieE4sSub1VU0FshgbektLOCvWnVnhqb1bFGc4v_ij6EC0h8pqJf0rYqKLt-OT2yLus2I10eicPie8GiBF4pFUHsC8i_GhuzYdlRBiUsf3Uote8ukNHDKEwzrvglQ2WccptnrZd-F6bC0HsWoHiS-UhAarjZDXT70KujAJ54HSM5n-XIvaQzLfZ6zoXX6gn4fqN9dteBHVxEvjEmBXl1eWGxDGd9woAC4wLjLA";

                    var token_credential = new CommunicationTokenCredential(userToken);
                    call_agent = await call_client.CreateCallAgent(token_credential, new CallAgentOptions());

                    call_agent.OnCallsUpdated += Agent_OnCallsUpdated;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog($"It was not possible to create call agent. Please check if token is valid:{Environment.NewLine}{ex.Message}").ShowAsync();
                return;
            }
        }

        private async Task CreateLocalVideoStream()
        {
            try
            {
                if (deviceManager.Cameras.Count > 0)
                {
                    var videoDeviceInfo = deviceManager.Cameras[0];
                    localVideoStream = new LocalVideoStream[1];
                    localVideoStream[0] = new LocalVideoStream(videoDeviceInfo);

                    var localUri = await localVideoStream[0].CreateBindingAsync();

                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        LocalVideo.Source = localUri;
                        LocalVideo.Play();
                    });

                }
            }
            catch (Exception ex)
            {
                await new MessageDialog($"Error creating local video stream:{Environment.NewLine}{ex.Message}").ShowAsync();
                return;
            }
        }

        private async Task JoinTeamsMeeting()
        {
            try
            {
                var joinCallOptions = new JoinCallOptions();
                joinCallOptions.VideoOptions = new VideoOptions(localVideoStream);
                var teamsMeetingLinkLocator = new TeamsMeetingLinkLocator(TeamsLinkTextBox.Text);
                call_ = await call_agent.JoinAsync(teamsMeetingLinkLocator, joinCallOptions);

                call_.OnStateChanged += Call_OnStateChangedAsync;
            }
            catch (Exception ex)
            {
                await new MessageDialog($"It was not possible to join the Teams meeting. Please check if Teams Link is valid:{Environment.NewLine}{ex.Message}").ShowAsync();
                return;
            }
        }


        private async void Call_OnStateChangedAsync(object sender, PropertyChangedEventArgs args)
        {
            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    CallStatusTextBlock.Text = call_.State.ToString();
                });

                switch (((Call)sender).State)
                {
                    case CallState.Disconnected:
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            LocalVideo.Stop();
                            LocalVideo.Source = null;
                            RemoteVideo.Stop();
                            RemoteVideo.Source = null;
                        });

                        localVideoStream[0].ReleaseBinding();
                        localVideoStream[0] = null;
                        break;
                    default:
                        Debug.WriteLine(((Call)sender).State);
                        break;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog($"Call_OnStateChangedAsync error:{Environment.NewLine}{ex.Message}").ShowAsync();
            }
        }

        private async void LeaveButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                if (call_ is null)
                {
                    return;
                }

                await call_.StopVideo(localVideoStream[0]);
                await call_.HangUpAsync(new HangUpOptions());
            }
            catch (Exception ex)
            {
                await new MessageDialog($"It was not possible to leave the Teams meeting:{Environment.NewLine}{ex.Message}").ShowAsync();
            }
        }

        private async Task<bool> ValidateInput()
        {
            if (TeamsLinkTextBox.Text.Trim().Length == 0 || !TeamsLinkTextBox.Text.StartsWith("http"))
            {
                await new MessageDialog("Please enter Teams meeting link.").ShowAsync();
                return false;
            }

            return true;
        }

        private async void Agent_OnCallsUpdated(object sender, CallsUpdatedEventArgs args)
        {
            foreach (var call in args.AddedCalls)
            {
                foreach (var remoteParticipant in call.RemoteParticipants)
                {
                    await AddVideoStreams(remoteParticipant.VideoStreams);
                    remoteParticipant.OnVideoStreamsUpdated += async (s, a) => await AddVideoStreams(a.AddedRemoteVideoStreams);
                }
                call.OnRemoteParticipantsUpdated += Call_OnRemoteParticipantsUpdated;
                call.OnStateChanged += Call_OnStateChangedAsync;
            }
        }

        private async void Call_OnRemoteParticipantsUpdated(object sender, ParticipantsUpdatedEventArgs args)
        {
            foreach (var remoteParticipant in args.AddedParticipants)
            {
                await AddVideoStreams(remoteParticipant.VideoStreams);
                remoteParticipant.OnVideoStreamsUpdated += async (s, a) => await AddVideoStreams(a.AddedRemoteVideoStreams);
            }
        }

        private async Task AddVideoStreams(IReadOnlyList<RemoteVideoStream> streams)
        {

            foreach (var remoteVideoStream in streams)
            {
                var remoteUri = await remoteVideoStream.CreateBindingAsync();

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    RemoteVideo.Source = remoteUri;
                    RemoteVideo.Play();
                });
                remoteVideoStream.Start();
            }
        }
    }


}
