﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BudgetAnalyser.Engine;
using BudgetAnalyser.UnitTest.TestHarness;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BudgetAnalyser.UnitTest.ApplicationHooks
{
    [TestClass]
    public class BatchFileApplicationHookSubscriberTest
    {
        private IList<Mock<IApplicationHookEventPublisher>> MockPublishers { get; set; }

        private IEnumerable<IApplicationHookEventPublisher> Publishers
        {
            get { return MockPublishers.Select(m => m.Object); }
        }

        [TestInitialize]
        public void TestInitialise()
        {
            MockPublishers = new List<Mock<IApplicationHookEventPublisher>>
            {
                new Mock<IApplicationHookEventPublisher>(),
            };
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SubscribeShouldThrowIfPublishersIsNull()
        {
            var subject = Arrange();
            subject.Subscribe(null);
            Assert.Fail();
        }
        
        [TestMethod]
        public void SubscribeShouldSubscribeAndRespondToPublisherEvent()
        {
            var subject = Arrange();
            bool eventCalled = false;
            Task internalTask = null;
            subject.PerformActionOverride = (s, e) => internalTask = Task.Factory.StartNew(() => eventCalled = true);
            
            subject.Subscribe(Publishers);
            RaisePublisherEvents();

            internalTask.Wait();
            Assert.IsTrue(eventCalled);
        }

        private void RaisePublisherEvents()
        {
            MockPublishers.ToList().ForEach(p => p.Raise(m => m.ApplicationEvent += null, new ApplicationHookEventArgs(ApplicationHookEventType.Application, "TestOrigin", "TestSubcategory")));
        }

        private BatchFileApplicationHookSubscriberTestHarness Arrange()
        {
            return new BatchFileApplicationHookSubscriberTestHarness();
        }
    }
}
