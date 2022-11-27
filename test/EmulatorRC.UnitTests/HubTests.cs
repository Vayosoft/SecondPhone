//using System.Dynamic;
//using EmulatorRC.API.Snippets.Hubs;
//using Microsoft.AspNetCore.SignalR;
//using Moq;
//using Xunit.Abstractions;

//namespace EmulatorRC.IntegrationTests
//{
//    public class HubTests
//    {
//        private readonly ITestOutputHelper _logger;

//        public HubTests(ITestOutputHelper logger)
//        {
//            _logger = logger;
//        }

//        [Fact]
//        public async Task HubsAreMockableViaDynamic()
//        {
//            bool sendCalled = false;
//            var hub = new ChatHub();
//            var mockClients = new Mock<IHubCallerClients>();
//            hub.Clients = mockClients.Object;
//            dynamic all = new ExpandoObject();
//            all.ReceiveMessage = new Action<string, string>((name, message) => {
//                sendCalled = true;
//            });

//            mockClients.Setup(m => m.All).Returns(() => mockClients.Object);

//            await hub.SendMessage("TestUser", "TestMessage");

//            Assert.True(sendCalled);
//        }

//        [Fact]
//        public async Task SendNotification()
//        {
//            var sendCalled = false;
//            var hub = new ChatHub();

//            var mockClients = new Mock<IHubCallerClients>();
//            var mockClientProxy = new Mock<IClientProxy>();

//            hub.Clients = mockClients.Object;
//            dynamic all = new ExpandoObject();
//            all.ReceiveMessage = new Action<string, string>((name, message) => {
//                _logger.WriteLine(name);
//                _logger.WriteLine(message);
//                sendCalled = true;
//            });

//            mockClients.Setup(clients => clients.All).Returns(mockClientProxy.Object);
//            await hub.SendMessage("TestUser", "TestMessage");

//            //var hubContext = new Mock<IHubContext<ChatHub>>();
//            //hubContext.Setup(x => x.Clients).Returns(() => mockClients.Object);

//            //var db = MyDBMock.GetMock();
//            //MyHubService hub = new MyHubService(hubContext.Object);

//            mockClients.Verify(clients => clients.All, Times.Once);

//            Assert.True(sendCalled);
//        }
//    }
//}