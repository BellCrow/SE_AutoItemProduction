using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.ModAPI.Ingame;
using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI.Interfaces;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Library.Collections;
using IMyGridTerminalSystem = Sandbox.ModAPI.IMyGridTerminalSystem;

namespace PartWatcher_alpha
{
    public class Programcode
    {
        #region DONT COPY

        private static IMyGridTerminalSystem GridTerminalSystem;

        private static void Echo(string msg)
        {
        }

        #endregion

        private const bool DEBUG = false;
        private const string CONTROL_BLOCK_PREFIX = "c_";

        private const string LCD_DISPLAY_NAME = "c_display";
        private const string CONTAINER_NAME = "c_container";
           
        //called every millisecond or so
        public void Main()
        {
            //get lcd first
            var container = new Container(GridTerminalSystem,CONTAINER_NAME);
            var assembler = new Assembler(GridTerminalSystem,"c_assembler");
            Echo(container.GetItemCount(Item.ITEM.STEEL_PLATE).ToString());
            var lcd = (IMyTextPanel) GridTerminalSystem.GetBlockWithName(LCD_DISPLAY_NAME);
            //itemType quota
            var quotaTable = new Dictionary<Item.ITEM, int> {{Item.ITEM.STEEL_PLATE, 50}};

            foreach (var quota in quotaTable)
            {
                var existingItemCount = container.GetItemCount(quota.Key);
                var toEnqueue = quota.Value - existingItemCount;
                lcd.WritePublicText("Enqueing " + toEnqueue + " items");
                assembler.EnqueueToSatisfyQuota(quota.Key,toEnqueue);
            }
        }

        //called only once. can be used to init
        public Programcode()
        {

        }

        #region Space engineers abstractions
        public class Container
        {
            #region Consts

            private const string CONTAINER_NOT_FOUND = "Could not get Container with given name";

            #endregion

            private IMyCargoContainer _container;

            private IMyGridTerminalSystem _gts;

            public Container(IMyGridTerminalSystem gts, string containerName)
            {
                _gts = gts;
                _container = _gts.GetBlockWithName(containerName) as IMyCargoContainer;
                if (_container == null)
                {
                    Util.FatalError(CONTAINER_NOT_FOUND);
                }
            }

            public int GetDistinctItems()
            {
                var items = GetItems();
                var ret = new List<Item>();

                foreach (var item in items)
                {
                    if (ret.Contains(item))
                        continue;
                    ret.Add(item);
                }
                return ret.Count;
            }

            public int GetItemCount()
            {
                return GetItems().Count;
            }

            public List<Item> GetItems()
            {
                var ret = new List<Item>();
                int inventory = 0;
                foreach(var rawItem in _container.GetInventory(inventory).GetItems()) 
                    ret.Add(new Item(rawItem));
                return ret;
            }

            public int GetItemCount(Item.ITEM searcheditem)
            {
                
                var items = GetItems();

                return Util.CountItemInInventory(items, searcheditem);
            }
        }

        public class Item
        {

            #region itemenum
            public enum ITEM
            {
                CONTRUCTION_COMPONENT,
                COMPUTER,
                DISPLAY,
                METALGRID,
                INTERIOR_PLATE,
                STEEL_PLATE,
                SMALL_STEEL_TUBE,
                LARGE_STEEL_TUBE,
                BULLETPROOF_GLASS,
                REACTOR_COMPONENT,
                THRUSTER_COMPONENT,
                GRAVGEN_COMPONENT,
                MEDICAL_COMPONENT,
                RADIO_COMPONENT,
                DETECTOR_COMPONENT,
                SOLAR_CELL,
                POWER_CELL,
                RIFLE,
                ROCKET_LAUNCHER,
                WELDER,
                GRINDER,
                HAND_DRILL,

                //ORES
                STONE,
                IRON_ORE,
                COBALT_ORE,
                SILICON_ORE,
                SILVER_ORE,
                GOLD_ORE,
                PLATINUM_ORE,
                URANIUM_ORE,
                MAGNESIUM_ORE,
                NICKEL_ORE,
                ICE,

                //BARS
                GRAVEL,
                IRON_INGOT,
                COBALT_INGOT,
                MAGNESIUM_POWDER,
                NICKEL_INGOT,
                SILICON_WAFER,
                SILVER_INGOT,
                GOLD_INGOT,
                PLATINUM_INGOT,
                URANIUM_INGOT,

                //META 
                NOT_SUPPORTED_OR_UNKNOWN
            }

            #endregion

            public Item(IMyInventoryItem rawItem)
            {
                _rawItem = rawItem;
                itemType = ConvertItemObjToItem(_rawItem);
            }

            public ITEM itemType;

            public int amount;

            private IMyInventoryItem _rawItem;

            //function shamelessly stolen from http://thefinalfrontier.se/cargo-container-inventory/
            public static string DecodeItemName(IMyInventoryItem item)
            {

                var name = item.Content.SubtypeName;
                var typeId = item.Content.TypeId.ToString();

                if (name.Equals("Construction"))
                {
                    return "Construction Component";
                }

                if (name.Equals("MetalGrid"))
                {
                    return "Metal Grid";
                }

                if (name.Equals("InteriorPlate"))
                {
                    return "Interior Plate";
                }

                if (name.Equals("SteelPlate"))
                {
                    return "Steel Plate";
                }

                if (name.Equals("SmallTube"))
                {
                    return "Small Steel Tube";
                }

                if (name.Equals("LargeTube"))
                {
                    return "Large Steel Tube";
                }

                if (name.Equals("BulletproofGlass"))
                {
                    return "Bulletproof Glass";
                }

                if (name.Equals("Reactor"))
                {
                    return "Reactor Component";
                }

                if (name.Equals("Thrust"))
                {
                    return "Thruster Component";
                }

                if (name.Equals("GravityGenerator"))
                {
                    return "GravGen Component";
                }

                if (name.Equals("Medical"))
                {
                    return "Medical Component";
                }

                if (name.Equals("RadioCommunication"))
                {
                    return "Radio Component";
                }

                if (name.Equals("Detector"))
                {
                    return "Detector Component";
                }

                if (name.Equals("SolarCell"))
                {
                    return "Solar Cell";
                }

                if (name.Equals("PowerCell"))
                {
                    return "Power Cell";
                }

                if (name.Equals("AutomaticRifleItem"))
                {
                    return "Rifle";
                }

                if (name.Equals("AutomaticRocketLauncher"))
                {
                    return "Rocket Launcher";
                }

                if (name.Equals("WelderItem"))
                {
                    return "Welder";
                }

                if (name.Equals("AngleGrinderItem"))
                {
                    return "Grinder";
                }

                if (name.Equals("HandDrillItem"))
                {
                    return "Hand Drill";
                }

                if (typeId.EndsWith("_Ore"))
                {
                    if (name.Equals("Stone"))
                    {
                        return name;
                    }

                    return name + " Ore";
                }

                if (typeId.EndsWith("_Ingot"))
                {
                    if (name.Equals("Stone"))
                    {
                        return "Gravel";
                    }

                    if (name.Equals("Magnesium"))
                    {
                        return name + " Powder";
                    }

                    if (name.Equals("Silicon"))
                    {
                        return name + " Wafer";
                    }

                    return name + " Ingot";
                }

                return name;
            }

            public static ITEM ConvertItemObjToItem(IMyInventoryItem item)
            {
                return ConvertSubTypeAndTypeIdToItem(item.Content.SubtypeName, item.Content.SubtypeId.ToString());
            }

            public static ITEM ConvertSubTypeAndTypeIdToItem(string subTypeString, string typeId)
            {
                var name = subTypeString;

                if (name.Equals("Construction")) { return ITEM.CONTRUCTION_COMPONENT; }
                if (name.Equals("MetalGrid")) { return ITEM.METALGRID; }
                if (name.Equals("InteriorPlate")) { return ITEM.INTERIOR_PLATE; }

                if (name.Equals("SteelPlate")) { return ITEM.STEEL_PLATE; }
                if (name.Equals("SmallTube")) { return ITEM.SMALL_STEEL_TUBE; }
                if (name.Equals("LargeTube")) { return ITEM.LARGE_STEEL_TUBE; }
                if (name.Equals("BulletproofGlass")) { return ITEM.BULLETPROOF_GLASS; }
                if (name.Equals("Reactor")) { return ITEM.REACTOR_COMPONENT; }
                if (name.Equals("Thrust")) { return ITEM.THRUSTER_COMPONENT; }

                if (typeId.EndsWith("_Ore"))
                {
                    if (name.Equals("Stone")) { return ITEM.STONE; }
                    if (name.Equals("Iron")) { return ITEM.IRON_ORE; }
                    if (name.Equals("Nickel")) { return ITEM.NICKEL_ORE; }
                    if (name.Equals("Cobalt")) { return ITEM.COBALT_ORE; }
                    if (name.Equals("Magnesium")) { return ITEM.MAGNESIUM_ORE; }
                    if (name.Equals("Silicon")) { return ITEM.SILICON_ORE; }
                    if (name.Equals("Silver")) { return ITEM.SILVER_ORE; }
                    if (name.Equals("Gold")) { return ITEM.GOLD_ORE; }
                    if (name.Equals("Platinum")) { return ITEM.PLATINUM_ORE; }
                    if (name.Equals("Uranium")) { return ITEM.URANIUM_ORE; }
                }
                if (typeId.EndsWith("_Ingot"))
                {
                    if (name.Equals("Stone")) { return ITEM.GRAVEL; }
                    if (name.Equals("Iron")) { return ITEM.IRON_INGOT; }
                    if (name.Equals("Nickel")) { return ITEM.NICKEL_INGOT; }
                    if (name.Equals("Cobalt")) { return ITEM.COBALT_INGOT; }
                    if (name.Equals("Magnesium")) { return ITEM.MAGNESIUM_POWDER; }
                    if (name.Equals("Silicon")) { return ITEM.SILICON_WAFER; }
                    if (name.Equals("Silver")) { return ITEM.SILVER_INGOT; }
                    if (name.Equals("Gold")) { return ITEM.GOLD_INGOT; }
                    if (name.Equals("Platinum")) { return ITEM.PLATINUM_INGOT; }
                    if (name.Equals("Uranium")) { return ITEM.URANIUM_INGOT; }
                }
                return ITEM.NOT_SUPPORTED_OR_UNKNOWN;
            }

            public override string ToString()
            {
                return _rawItem.Content.TypeId.ToString() + "-" + _rawItem.Content.SubtypeName;
            }

            public string GetLcdString()
            {
                return DecodeItemName(_rawItem);
            }

            public override bool Equals(object obj)
            {
                var cast = obj as Item;
                if (cast == null)
                    return false;
                return cast.itemType == itemType;
            }

            public bool Equals(ITEM argItemType)
            {
                return argItemType == itemType;
            }
        }

        public class Assembler
        {
            
            #region assembler blueprint strings
            //itemType string stolen from https://steamcommunity.com/app/244850/discussions/0/527273452877873614/
            const string BulletproofGlass = "MyObjectBuilder_BlueprintDefinition/BulletproofGlass";
            const string ComputerComponent = "MyObjectBuilder_BlueprintDefinition/ComputerComponent";
            const string ConstructionComponent = "MyObjectBuilder_BlueprintDefinition/ConstructionComponent";
            const string DetectorComponent = "MyObjectBuilder_BlueprintDefinition/DetectorComponent";
            const string Display = "MyObjectBuilder_BlueprintDefinition/Display";
            const string ExplosivesComponent = "MyObjectBuilder_BlueprintDefinition/ExplosivesComponent";
            const string GirderComponent = "MyObjectBuilder_BlueprintDefinition/GirderComponent";
            const string GravityGeneratorComponent = "MyObjectBuilder_BlueprintDefinition/GravityGeneratorComponent";
            const string InteriorPlate = "MyObjectBuilder_BlueprintDefinition/InteriorPlate";
            const string LargeTube = "MyObjectBuilder_BlueprintDefinition/LargeTube";
            const string MedicalComponent = "MyObjectBuilder_BlueprintDefinition/MedicalComponent";
            const string MetalGrid = "MyObjectBuilder_BlueprintDefinition/MetalGrid";
            const string Missile200mm = "MyObjectBuilder_BlueprintDefinition/Missile200mm";
            const string MotorComponent = "MyObjectBuilder_BlueprintDefinition/MotorComponent";
            const string NATO_25x184mmMagazine = "MyObjectBuilder_BlueprintDefinition/NATO_25x184mmMagazine";
            const string NATO_5p56x45mmMagazine = "MyObjectBuilder_BlueprintDefinition/NATO_5p56x45mmMagazine";
            const string PowerCell = "MyObjectBuilder_BlueprintDefinition/PowerCell";
            const string RadioCommunicationComponent = "MyObjectBuilder_BlueprintDefinition/RadioCommunicationComponent";
            const string ReactorComponent = "MyObjectBuilder_BlueprintDefinition/ReactorComponent";
            const string SmallTube = "MyObjectBuilder_BlueprintDefinition/SmallTube";
            const string SolarCell = "MyObjectBuilder_BlueprintDefinition/SolarCell";
            const string SteelPlate = "MyObjectBuilder_BlueprintDefinition/SteelPlate";
            const string Superconductor = "MyObjectBuilder_BlueprintDefinition/Superconductor";
            const string ThrustComponent = "MyObjectBuilder_BlueprintDefinition/ThrustComponent";
            const string AngleGrinder = "MyObjectBuilder_BlueprintDefinition/AngleGrinder";
            const string AngleGrinder2 = "MyObjectBuilder_BlueprintDefinition/AngleGrinder2";
            const string AngleGrinder3 = "MyObjectBuilder_BlueprintDefinition/AngleGrinder3";
            const string AngleGrinder4 = "MyObjectBuilder_BlueprintDefinition/AngleGrinder4";
            const string HandDrill = "MyObjectBuilder_BlueprintDefinition/HandDrill";
            const string HandDrill2 = "MyObjectBuilder_BlueprintDefinition/HandDrill2";
            const string HandDrill3 = "MyObjectBuilder_BlueprintDefinition/HandDrill3";
            const string HandDrill4 = "MyObjectBuilder_BlueprintDefinition/HandDrill4";
            const string Welder = "MyObjectBuilder_BlueprintDefinition/Welder";
            const string Welder2 = "MyObjectBuilder_BlueprintDefinition/Welder2";
            const string Welder3 = "MyObjectBuilder_BlueprintDefinition/Welder3";
            const string Welder4 = "MyObjectBuilder_BlueprintDefinition/Welder4";
            const string AutomaticRifle = "MyObjectBuilder_BlueprintDefinition/AutomaticRifle";
            const string PreciseAutomaticRifle = "MyObjectBuilder_BlueprintDefinition/PreciseAutomaticRifle";
            const string RapidFireAutomaticRifle = "MyObjectBuilder_BlueprintDefinition/RapidFireAutomaticRifle";
            const string UltimateAutomaticRifle = "MyObjectBuilder_BlueprintDefinition/UltimateAutomaticRifle";
            const string HydrogenBottle = "MyObjectBuilder_BlueprintDefinition/HydrogenBottle";
            const string OxygenBottl = "MyObjectBuilder_BlueprintDefinition/OxygenBottle";

            #endregion

            private const string ASSEMBLER_NOT_FOUND = "Could not find Assembler with given Name";

            private IMyGridTerminalSystem _gts;
            private readonly IMyAssembler _assembler;

            public IMyAssembler GetRawAssembler()
            {
                return _assembler;
            }
            
            public Assembler(IMyGridTerminalSystem gts,string assemblername)
            {
                _gts = gts;
                _assembler = (IMyAssembler)_gts.GetBlockWithName(assemblername);
                if(_assembler == null)
                { Util.FatalError(ASSEMBLER_NOT_FOUND); }
            }

            public bool MaterialMissing()
            {
                return !_assembler.IsProducing &&
                       !_assembler.IsQueueEmpty &&
                        _assembler.IsWorking;
            }

            public void CancelCurrentlyProducedItem()
            {
                if (_assembler.IsQueueEmpty)
                    return;
                var currentlyProducedItems = new List<MyProductionItem>();
                _assembler.GetQueue(currentlyProducedItems);
                _assembler.RemoveQueueItem(0, currentlyProducedItems[0].Amount);
            }

            public void MoveCurrentlyProducedItemToEndOfQueue()
            {
                if (_assembler.IsQueueEmpty)
                    return;
                var currentlyProducedItems = new List<MyProductionItem>();

                _assembler.GetQueue(currentlyProducedItems);
                var currentlyProducedItem = currentlyProducedItems[0];
                _assembler.RemoveQueueItem(0, currentlyProducedItems[0].Amount);
                _assembler.AddQueueItem(currentlyProducedItem.BlueprintId,currentlyProducedItem.Amount);
            }

            public void EnqueueItem(Item.ITEM toBuild, long amount)
            {
                if (amount < 1)
                    return;
                MyDefinitionId bluePrint;

                #region builditem switch
                switch (toBuild)
                    {
                        case Item.ITEM.CONTRUCTION_COMPONENT:
                            bluePrint = MyDefinitionId.Parse(ConstructionComponent);
                            break;
                        case Item.ITEM.COMPUTER:
                            bluePrint = MyDefinitionId.Parse(ComputerComponent);
                            break;
                        case Item.ITEM.DISPLAY:
                            bluePrint = MyDefinitionId.Parse(Display);
                            break;
                        case Item.ITEM.METALGRID:
                            bluePrint = MyDefinitionId.Parse(MetalGrid);
                            break;
                        case Item.ITEM.INTERIOR_PLATE:
                            bluePrint = MyDefinitionId.Parse(InteriorPlate);
                            break;
                        case Item.ITEM.STEEL_PLATE:
                            bluePrint = MyDefinitionId.Parse(SteelPlate);
                            break;
                        case Item.ITEM.SMALL_STEEL_TUBE:
                            bluePrint = MyDefinitionId.Parse(SmallTube);
                            break;
                        case Item.ITEM.LARGE_STEEL_TUBE:
                            bluePrint = MyDefinitionId.Parse(LargeTube);
                            break;
                        case Item.ITEM.BULLETPROOF_GLASS:
                            bluePrint = MyDefinitionId.Parse(BulletproofGlass);
                            break;
                        case Item.ITEM.REACTOR_COMPONENT:
                            bluePrint = MyDefinitionId.Parse(ReactorComponent);
                            break;
                        case Item.ITEM.THRUSTER_COMPONENT:
                            bluePrint = MyDefinitionId.Parse(ThrustComponent);
                            break;
                        case Item.ITEM.GRAVGEN_COMPONENT:
                            bluePrint = MyDefinitionId.Parse(GravityGeneratorComponent);
                            break;
                        case Item.ITEM.MEDICAL_COMPONENT:
                            bluePrint = MyDefinitionId.Parse(MedicalComponent);
                            break;
                        case Item.ITEM.RADIO_COMPONENT:
                            bluePrint = MyDefinitionId.Parse(RadioCommunicationComponent);
                            break;
                        case Item.ITEM.DETECTOR_COMPONENT:
                            bluePrint = MyDefinitionId.Parse(DetectorComponent);
                            break;
                        case Item.ITEM.SOLAR_CELL:
                            bluePrint = MyDefinitionId.Parse(SolarCell);
                            break;
                        case Item.ITEM.POWER_CELL:
                            bluePrint = MyDefinitionId.Parse(PowerCell);
                            break;
                        
                        default:
                            throw new Exception("Unknown Item to build given");
                
                    }
                #endregion

                _assembler.AddQueueItem(bluePrint, (decimal) amount);
            }

            /// <summary>
            /// Will take the given itemType and will enqueue as much as is need to satisfy the given qouta.
            /// That means, if a qouta of 40 is given and there are already 20 items of this kind enqueued.
            /// the method will issu only an additional 20 items
            /// </summary>
            /// <param name="toBuild">The itemType to build</param>
            /// <param name="quotaAmount">The quota needed to be satisfied</param>
            public void EnqueueToSatisfyQuota(Item.ITEM toBuild, long quotaAmount)
            {
                EnqueueItem(toBuild,quotaAmount - GetEnqueuedItemsOfType(toBuild));
            }

            /// <summary>
            /// Will search the Asemblers queue for the given itemType and returns the 
            /// </summary>
            /// <param name="itemType"></param>
            /// <returns></returns>
            public long GetEnqueuedItemsOfType(Item.ITEM itemType)
            {
                var queuedItems = new List<MyProductionItem>();
                _assembler.GetQueue(queuedItems);
                long retAmount = 0;

                foreach (var iterItem in queuedItems)
                {
                    if (Item.ConvertSubTypeAndTypeIdToItem(iterItem.BlueprintId.SubtypeName, iterItem.BlueprintId.SubtypeId.ToString()) == itemType)
                    {
                        retAmount += iterItem.Amount.ToIntSafe();
                    }
                }
                return retAmount;
            }
            private List<Item> GetInventory(bool getOutputInventory)
            {
                var ret = new List<Item>();
                foreach (var item in _assembler.GetInventory(getOutputInventory?1:0).GetItems())
                {
                    ret.Add(new Item(item));
                }

                return ret;
            }
            public List<Item> GetOutputInventory()
            {
                return GetInventory(true);
            }
            public List<Item> GetInputInventory()
            {
                return GetInventory(false);
            }

        }

        #endregion

        public static class Util
        {
            public static void FatalError(string errorMessage)
            {
                throw new Exception(errorMessage);
            }

            public static int CountItemInInventory(List<Item> toCountIn,Item.ITEM toCount)
            {
                var itemCount = 0;
                foreach (var item in toCountIn)
                {
                    if (item.itemType == toCount)
                        itemCount += item.amount;
                }
                return itemCount;
            }
        }
    }
}