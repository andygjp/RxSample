namespace RxSample.UnitTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Reactive.Testing;
    using NSubstitute;
    using Xunit;

    // See http://blogs.msdn.com/b/rxteam/archive/2012/06/14/testing-rx-queries-using-virtual-time-scheduling.aspx
    // and http://www.introtorx.com/content/v1.0.10621.0/16_TestingRx.html helpful tips
    public class When_initialize_viewmodel
    {
        private readonly MainWindowViewModel _sut;
        private readonly INameService _service;

        public When_initialize_viewmodel()
        {
            // Setup test service with dummy data
            _service = Substitute.For<INameService>();
            _service.GetNames().Returns(Task.FromResult(new[] { "Hello", "World" }.AsEnumerable()));

            var testScheduler = new TestScheduler();

            _sut = new MainWindowViewModel(AsyncMethod.OnScheduler, _service, testScheduler);
            _sut.Initialize();

            // The service has been subscribed to and everything is setup - just need kick it off
            testScheduler.Start();
        }

        [Fact]
        public void It_should_add_service_data_to_collection()
        {
            _sut.Names.Should().HaveCount(2);
        }

        [Fact]
        public void It_should_get_data_from_service()
        {
            _service.Received().GetNames();
        }
    }
}
