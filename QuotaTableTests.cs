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

            var quotaTable = new p.QuotaTableFactory(containerMoq.Object);
            quotaTable.AddQuotaForItem(p.Item.ITEM.STEEL_PLATE, 50);
            quotaTable.AddQuotaForItem(p.Item.ITEM.CONSTRUCTION_COMPONENT, 50);
            quotaTable.AddQuotaForItem(p.Item.ITEM.GIRDER, 100);
            quotaTable.AddQuotaForItem(p.Item.ITEM.MOTOR, 30);


            var QuotaList = quotaTable.GetMissingItemQuota();

            Assert.AreEqual(4,QuotaList.Count);

            Assert.AreEqual(p.Item.ITEM.MOTOR,QuotaList.GetNextHighestPrioritizedEntry().ItemType);
            
            Assert.AreEqual(p.Item.ITEM.MOTOR, QuotaList.GetNextHighestPrioritizedEntry().ItemType);
            Assert.AreEqual(p.Item.ITEM.GIRDER, QuotaList.GetNextHighestPrioritizedEntry().ItemType);
        }

        [Test]
        public void FilterOutQuotaEntriesThatAreFulfilled()
        {
            var containerMoq = new Mock<p.Container>();
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.STEEL_PLATE)).Returns(50);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.CONSTRUCTION_COMPONENT)).Returns(90);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.GIRDER)).Returns(60);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.MOTOR)).Returns(1);

            var quotaTable = new p.QuotaTableFactory(containerMoq.Object);
            quotaTable.AddQuotaForItem(p.Item.ITEM.STEEL_PLATE, 50);
            quotaTable.AddQuotaForItem(p.Item.ITEM.CONSTRUCTION_COMPONENT, 50);
            quotaTable.AddQuotaForItem(p.Item.ITEM.GIRDER, 100);
            quotaTable.AddQuotaForItem(p.Item.ITEM.MOTOR, 30);


            var QuotaList = quotaTable.GetMissingItemQuota();
            Assert.AreEqual(2, QuotaList.Count);

            using (var enumerator = QuotaList.GetReadonlyQuotaList().GetEnumerator())
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(p.Item.ITEM.GIRDER, enumerator.Current.ItemType);
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(p.Item.ITEM.MOTOR, enumerator.Current.ItemType);
            }
            Assert.AreEqual(p.Item.ITEM.MOTOR, QuotaList.GetNextHighestPrioritizedEntry().ItemType);
            
            Assert.AreEqual(p.Item.ITEM.MOTOR, QuotaList.GetNextHighestPrioritizedEntry().ItemType);
            Assert.AreEqual(p.Item.ITEM.GIRDER, QuotaList.GetNextHighestPrioritizedEntry().ItemType);
        }

        [Test]
        public void RemoveQuotasThatAreMarkedAsUnbuildable()
        {
            var containerMoq = new Mock<p.Container>();
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.STEEL_PLATE)).Returns(45);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.CONSTRUCTION_COMPONENT)).Returns(45);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.GIRDER)).Returns(95);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.MOTOR)).Returns(1);

            var quotaTable = new p.QuotaTableFactory(containerMoq.Object);
            quotaTable.AddQuotaForItem(p.Item.ITEM.STEEL_PLATE, 50);
            quotaTable.AddQuotaForItem(p.Item.ITEM.CONSTRUCTION_COMPONENT, 50);
            quotaTable.AddQuotaForItem(p.Item.ITEM.GIRDER, 100);
            quotaTable.AddQuotaForItem(p.Item.ITEM.MOTOR, 900);

            var QuotaList = quotaTable.GetMissingItemQuota();

            Assert.AreEqual(4, QuotaList.Count);
            var highestPrio = QuotaList.GetNextHighestPrioritizedEntry();
            Assert.AreEqual(p.Item.ITEM.MOTOR, highestPrio.ItemType);
            highestPrio.CantBeBuilt = true;
            Assert.AreEqual(p.Item.ITEM.STEEL_PLATE, QuotaList.GetNextHighestPrioritizedEntry().ItemType);

        }

        [Test]
        public void IgnoreitesmWithNoMissingAmountAfterBuildEnqueing()
        {
            var containerMoq = new Mock<p.Container>();
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.STEEL_PLATE)).Returns(45);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.CONSTRUCTION_COMPONENT)).Returns(45);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.GIRDER)).Returns(95);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.MOTOR)).Returns(1);

            var quotaTable = new p.QuotaTableFactory(containerMoq.Object);
            quotaTable.AddQuotaForItem(p.Item.ITEM.STEEL_PLATE, 50);
            quotaTable.AddQuotaForItem(p.Item.ITEM.CONSTRUCTION_COMPONENT, 50);
            quotaTable.AddQuotaForItem(p.Item.ITEM.GIRDER, 100);
            quotaTable.AddQuotaForItem(p.Item.ITEM.MOTOR, 900);

            var QuotaList = quotaTable.GetMissingItemQuota();

            Assert.AreEqual(4, QuotaList.Count);
            var highestPrio = QuotaList.GetNextHighestPrioritizedEntry();
            Assert.AreEqual(p.Item.ITEM.MOTOR, highestPrio.ItemType);
            highestPrio.MissingAmount = 0;
            Assert.AreEqual(p.Item.ITEM.STEEL_PLATE, QuotaList.GetNextHighestPrioritizedEntry().ItemType);
        }

        [Test]
        public void ReturnNullIfAllQuotasAreFullFilled()
        {
            var containerMoq = new Mock<p.Container>();
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.STEEL_PLATE)).Returns(50);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.CONSTRUCTION_COMPONENT)).Returns(50);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.GIRDER)).Returns(100);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.MOTOR)).Returns(900);

            var quotaTable = new p.QuotaTableFactory(containerMoq.Object);
            quotaTable.AddQuotaForItem(p.Item.ITEM.STEEL_PLATE, 50);
            quotaTable.AddQuotaForItem(p.Item.ITEM.CONSTRUCTION_COMPONENT, 50);
            quotaTable.AddQuotaForItem(p.Item.ITEM.GIRDER, 100);
            quotaTable.AddQuotaForItem(p.Item.ITEM.MOTOR, 900);

            var QuotaList = quotaTable.GetMissingItemQuota();

            Assert.AreEqual(0, QuotaList.Count);
            var highestPrio = QuotaList.GetNextHighestPrioritizedEntry();
            Assert.IsNull(highestPrio);
            
        }
    }

    class AssemblerTest
    {
        [Test]
        public void GetProduceableitemAmount()
        {
            var containerMoq = new Mock<p.Container>();
            var rawAssemblerMoq = new Mock<IMyAssembler>();

            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.IRON_INGOT)).Returns(50);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.SILICON_WAFER)).Returns(10);

            var assembler = new p.Assembler(rawAssemblerMoq.Object,containerMoq.Object,null);
            var missingMaterial = new List<p.Item.ITEM>();

            Assert.AreEqual(7,assembler.GetProducableItemAmount(p.Item.ITEM.STEEL_PLATE,missingMaterial));
            Assert.AreEqual(0,missingMaterial.Count);
            missingMaterial.Clear();
            Assert.AreEqual(42,assembler.GetProducableItemAmount(p.Item.ITEM.INTERIOR_PLATE, missingMaterial));
            Assert.AreEqual(0, missingMaterial.Count);
            missingMaterial.Clear();
            Assert.AreEqual(5,assembler.GetProducableItemAmount(p.Item.ITEM.DISPLAY, missingMaterial));
            Assert.AreEqual(0, missingMaterial.Count);
            missingMaterial.Clear();
        }

        [Test]
        public void GetProduceAbleItemCountIfMaterialIsMissing()
        {
            var containerMoq = new Mock<p.Container>();
            var rawAssemblerMoq = new Mock<IMyAssembler>();

            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.IRON_INGOT)).Returns(50);
            containerMoq.Setup(container => container.GetItemCount(p.Item.ITEM.SILICON_WAFER)).Returns(10);

            var assembler = new p.Assembler(rawAssemblerMoq.Object, containerMoq.Object, null);
            var missingMaterial = new List<p.Item.ITEM>();

            Assert.AreEqual(0,assembler.GetProducableItemAmount(p.Item.ITEM.REACTOR_COMPONENT,missingMaterial));

            Assert.Contains(p.Item.ITEM.GRAVEL,missingMaterial);
            Assert.Contains(p.Item.ITEM.SILVER_INGOT,missingMaterial);
        }
    }
}
