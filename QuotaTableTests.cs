using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Moq;
using Sandbox.Game.GUI;
using Sandbox.ModAPI;
using p = PartWatcher_alpha.Program;

namespace PartWatcher_alpha.SePartWatcher
{
    [TestFixture]
    class QuotaTableTests
    {
        [Test]
        public void DequeueHighestPriorityWith2TimesSameItemAndThenADifferentOne()
        {
            var containerMoq = new Mock<p.Container>();
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.MOTOR)).Returns(1);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.CONSTRUCTION_COMPONENT)).Returns(40);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.GIRDER)).Returns(60);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.STEEL_PLATE)).Returns(40);

            var argList = new List<p.QuotaEntry>();
            argList.Add(new p.QuotaEntry(p.Item.ITEM.STEEL_PLATE, 50));
            argList.Add(new p.QuotaEntry(p.Item.ITEM.CONSTRUCTION_COMPONENT, 50));
            argList.Add(new p.QuotaEntry(p.Item.ITEM.GIRDER, 100));
            argList.Add(new p.QuotaEntry(p.Item.ITEM.MOTOR, 30));


            var _qTable = new p.QuotaTable(containerMoq.Object, argList);

            //get two times motor, as its still the top priority even after halfing its global priority
            Assert.AreEqual(p.Item.ITEM.MOTOR,_qTable.GetHighestPrioQuota().Item);
            Assert.AreEqual(p.Item.ITEM.MOTOR,_qTable.GetHighestPrioQuota().Item);
            Assert.AreEqual(p.Item.ITEM.GIRDER,_qTable.GetHighestPrioQuota().Item);
        }

        [Test]
        public void FilterOutQuotaEntriesThatAreFulfilled()
        {
            var containerMoq = new Mock<p.Container>();
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.MOTOR)).Returns(1);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.CONSTRUCTION_COMPONENT)).Returns(40);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.GIRDER)).Returns(60);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.STEEL_PLATE)).Returns(50);

            var argList = new List<p.QuotaEntry>();
            argList.Add(new p.QuotaEntry(p.Item.ITEM.STEEL_PLATE, 10));
            argList.Add(new p.QuotaEntry(p.Item.ITEM.CONSTRUCTION_COMPONENT, 50));
            argList.Add(new p.QuotaEntry(p.Item.ITEM.GIRDER, 100));
            argList.Add(new p.QuotaEntry(p.Item.ITEM.MOTOR, 30));


            var _qTable = new p.QuotaTable(containerMoq.Object, argList);

            //conservative guess at  which the steel plates should occur at least one time, no mater how low theire prio is
            for (int i = 0; i < 1000; i++)
            {
                Assert.AreNotEqual(p.Item.ITEM.STEEL_PLATE,_qTable.GetHighestPrioQuota().Item);
            }
        }

        [Test]
        public void RemoveQuotasThatAreMarkedAsUnbuildable()
        {
            var containerMoq = new Mock<p.Container>();
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.STEEL_PLATE)).Returns(10);

            var argList = new List<p.QuotaEntry>();
            argList.Add(new p.QuotaEntry(p.Item.ITEM.STEEL_PLATE, 50));

            var _qTable = new p.QuotaTable(containerMoq.Object, argList);

            Assert.AreEqual(_qTable.GetHighestPrioQuota().Item,p.Item.ITEM.STEEL_PLATE);

            _qTable.MarkLastHighestPriorityItemAsUnbuildabel();
            Assert.IsNull(_qTable.GetHighestPrioQuota());
        }
    }
}
