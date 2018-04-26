using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using IMyGridTerminalSystem = Sandbox.ModAPI.IMyGridTerminalSystem;

namespace PartWatcher_alpha
{
    public class Program
    {
        #region DONT COPY

        private static IMyGridTerminalSystem GridTerminalSystem;

        private static void Echo(string msg) { }

        public void Main1()
        {
            /*echodel = Echo;
            //var container = new Container(GridTerminalSystem, CONTAINER_NAME);
            //var assembler = new Assembler(GridTerminalSystem, "c_assembler");

            var lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(LCD_DISPLAY_NAME);

            var reporter = new RollingLcdReporter(lcd);

            for (int i = 0; i < 30; i++)
            {
                reporter.ReportInfo($"TestInfo + {i}");
                reporter.ReportWarning($"TestInfo + {i}");
                reporter.ReportSoftError($"TestInfo + {i}");
                reporter.ReportHardError($"TestInfo + {i}");
            }

            
            //itemType quota
            var quotaTable = new Dictionary<Item.ITEM, int> {
                { Item.ITEM.CONSTRUCTION_COMPONENT,20},
                { Item.ITEM.COMPUTER_COMPONENTS,20},
                { Item.ITEM.DISPLAY,20},
                { Item.ITEM.METALGRID,20},
                { Item.ITEM.INTERIOR_PLATE,20},
                { Item.ITEM.STEEL_PLATE,20},
                { Item.ITEM.SMALL_STEEL_TUBE,20},
                { Item.ITEM.LARGE_STEEL_TUBE,20},
                { Item.ITEM.BULLETPROOF_GLASS,20},
                { Item.ITEM.REACTOR_COMPONENT,20},
                { Item.ITEM.THRUSTER_COMPONENT,20},
                { Item.ITEM.GRAVGEN_COMPONENT,20},
                { Item.ITEM.MEDICAL_COMPONENT,20},
                { Item.ITEM.RADIO_COMPONENT,20},
                { Item.ITEM.DETECTOR_COMPONENT,20},
                { Item.ITEM.SOLAR_CELL,20},
                { Item.ITEM.POWER_CELL,20}
            };

            lcd.WritePublicText("Qoutas\n--------------------------------\n");

            foreach (var quota in quotaTable)
            {
                var existingItemCount = container.GetItemCount(quota.Key);
                existingItemCount += Util.CountItemInInventory(assembler.GetOutputInventory(), quota.Key);
                var toEnqueue = quota.Value - existingItemCount;
                var enqueued = assembler.GetEnqueuedItemsOfType(quota.Key);
                if (toEnqueue > 0)
              
            {
                    assembler.EnqueueToSatisfyQuota(quota.Key, toEnqueue);
                }
                printQuotaEntry(quota, existingItemCount, $"{toEnqueue} in Queue", lcd);
            }
            */
        }

        public void printQuotaEntry(KeyValuePair<Item.ITEM, int> quotaEntry, int currentItemCount, string actionText, IMyTextPanel textPanel)
        {
            textPanel.WritePublicText($"{Item.ConvertItemTypeToString(quotaEntry.Key)}: {currentItemCount}/{quotaEntry.Value}->{actionText}\n", true);
        }


        #endregion


        #region copy
        private static System.Action<string> echodel;


        //called every millisecond or so
        public void Main()
        {
            echodel = Echo;
            const string cargoFromName = "c_cargoIn";
            const string cargoToName = "c_cargoOut";

            const string debugpanelName = "c_debugPanel";
            const string errorPanelName = "c_debugPanel";
            const string assemblerPrefix = "c_assembler";

            var infoLogger = new RollingLcdReporter(GridTerminalSystem.GetBlockWithName(debugpanelName) as IMyTextPanel);
            var errorLogger = new RollingLcdReporter(GridTerminalSystem.GetBlockWithName(errorPanelName) as IMyTextPanel);
            var cargoFrom = new Container(GridTerminalSystem.GetBlockWithName(cargoFromName) as IMyCargoContainer);
            var cargoTo = new Container(GridTerminalSystem.GetBlockWithName(cargoToName) as IMyCargoContainer);

            var quotatable = new QuotaTable();
            quotatable.quotaList.Add(new QuotaEntry());
            var rawAssembler = new List<IMyAssembler>();
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(rawAssembler,
                myAssembler => myAssembler.CustomName.StartsWith(assemblerPrefix));

            var assemblerFarm = new AssemblerFarm(quotatable, cargoFrom, cargoTo, rawAssembler, infoLogger, errorLogger);
            assemblerFarm.EmptyAssemblerOutPuts();
        }

        //called only once. can be used to init on server start
        public Program(){}

        #region quota data
        public class QuotaTable
        {
            public List<QuotaEntry> quotaList;
            public QuotaTable()
            {
                quotaList = new List<QuotaEntry>();
            }

            public double GetGlobalPriority(int index)
            {
                return quotaList[index].PriorityPercent / PriorityPercentSum;
            }

            private double PriorityPercentSum
            {
                get
                {
                    double global = 0;
                    foreach (var entry in quotaList)
                        global += entry.PriorityPercent;
                    return global;
                }
            }
        }

        public class QuotaEntry
        {
            public readonly Item.ITEM item;
            public double quota;
            public double currentCount;
            public double PercentQuota => currentCount / quota;
            public double PriorityPercent => 1 - PercentQuota;
        }
        #endregion
        public class AssemblerFarm
        {
            private readonly Reporter _infoLogger;
            private readonly Reporter _errorLogger;
            private readonly Container _resourcePool;
            private readonly Container _targetPool;
            private QuotaTable _qTable;
            private List<Assembler> _assemblers;

            public AssemblerFarm(QuotaTable qTable,
                                Container resourcePool,
                                Container targetPool,
                                List<IMyAssembler> rawAssemblers,
                                Reporter infoLogger,
                                Reporter errorLogger)
            {
                _errorLogger = errorLogger;
                if (_errorLogger == null)
                {
                    Util.FatalError("Mull object as error-reporter supplied in Assemblerfarm");
                }
                _infoLogger = infoLogger;
                if (_infoLogger == null)
                {
                    _errorLogger.ReportSoftError("Null object supplied as infologger supplied in Assemblerfarm");
                }
                _qTable = qTable;
                if (_qTable?.quotaList?.Count < 1)
                {
                    _errorLogger.ReportSoftError("Supplied null or empty Qoutatable");
                }
                _targetPool = targetPool;
                if (_targetPool == null)
                {
                    _errorLogger.ReportSoftError("Supplied null as Targetbox");
                }
                _resourcePool = resourcePool;
                if (resourcePool == null)
                {
                    _errorLogger.ReportSoftError("Supplied null as Resourcepool");
                }

                if (_assemblers?.Count < 1)
                {
                    _errorLogger.ReportSoftError("Supplied null or empty list of Rawassemblers");
                }

                _assemblers = new List<Assembler>();
                foreach (var assembler in rawAssemblers)
                {
                    var managedAssembler = new Assembler(assembler, _resourcePool);
                    infoLogger.ReportInfo($"Initializing assembler {managedAssembler.AssemblerName}");
                    _assemblers.Add(managedAssembler);
                }

            }

            public void CheckQuotas()
            {
                EmptyAssemblerOutPuts();
            }

            public void EmptyAssemblerOutPuts()
            {
                foreach (var assembler in _assemblers)
                {
                    if (!assembler.EmptyOutputToInventory(_targetPool.RawContainer.GetInventory(0)))
                    {
                        _errorLogger.ReportSoftError($"Error emptieing output of Assembler {assembler.AssemblerName}");
                    }
                }
            }
        }


        #region Space engineers abstractions
        public class Container
        {

            private object sync = new object();

            public IMyCargoContainer RawContainer { get; }

            public Container(IMyCargoContainer cargoContainer)
            {
                if (cargoContainer == null)
                { Util.FatalError("Supplied null Argument to container constructor"); }
                RawContainer = cargoContainer;
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
                foreach (var rawItem in RawContainer.GetInventory(inventory).GetItems())
                    ret.Add(new Item(rawItem));
                return ret;
            }

            public int GetItemCount(Item.ITEM searcheditem)
            {
                var items = GetItems();
                return Util.CountItemInInventory(items, searcheditem);
            }

            /// <summary>
            /// Will merge all items of the same type into one stack
            /// </summary>
            public void MergeItemStacks()
            {
                var ditinctItemTypes = GetDistinctItemTypes();

                foreach (var itemType in ditinctItemTypes)
                {
                    MergeItemToStack(itemType);
                }
            }

            public void MergeItemToStack(Item.ITEM itemType)
            {
                int firstFound = -1;
                // find first stack index
                for (int i = 0; i < RawContainer.GetInventory().GetItems().Count; i++)
                {
                    IMyInventoryItem item = RawContainer.GetInventory().GetItems()[i];
                    if (Item.ConvertItemObjToItem(item) == itemType)
                    {
                        echodel.Invoke($"{itemType.ToString()} found on index {i}");
                        firstFound = i;
                        break;
                    }
                }

                // Merge other stack onto first one
                for (int i = (firstFound + 1); i < RawContainer.GetInventory().GetItems().Count; i++)
                {
                    IMyInventoryItem item = RawContainer.GetInventory().GetItems()[i];
                    if (Item.ConvertItemObjToItem(item) == itemType)
                    {
                        echodel.Invoke($"Found mergeable stack at {i}");
                        RawContainer.GetInventory().TransferItemTo(RawContainer.GetInventory(), i, firstFound, true);
                        i--;
                    }
                }
            }

            public List<Item.ITEM> GetDistinctItemTypes()
            {
                var ret = new List<Item.ITEM>();

                var inventory = RawContainer.GetInventory(0).GetItems();

                foreach (var item in inventory)
                {
                    var itemInstance = new Item(item);
                    if (!ret.Contains(itemInstance.itemType))
                    {
                        ret.Add(itemInstance.itemType);
                    }
                }
                return ret;
            }

            public int GetNextItemIndexOfAmount(Item.ITEM searchedItem, double amount)
            {
                // try to merge searched item before
                MergeItemToStack(searchedItem);

                for (int i = 0; i < RawContainer.GetInventory().GetItems().Count; i++)
                {
                    IMyInventoryItem item = RawContainer.GetInventory().GetItems()[i];
                    if (Item.ConvertItemObjToItem(item) == searchedItem && item.Amount >= (VRage.MyFixedPoint)amount)
                        return i;
                }
                return -1;
            }

            public bool MoveResourcesTo(Item.ITEM desiredMaterial, double amount, IMyInventory targetInventory)
            {
                lock (sync)
                {
                    if (targetInventory == null
                                || GetItemCount(desiredMaterial) < amount
                                || !RawContainer.GetInventory(0).IsConnectedTo(targetInventory)
                                || targetInventory.IsFull)
                    {
                        return false;
                    }

                    MergeItemStacks();

                    int sourceIndex = GetNextItemIndexOfAmount(desiredMaterial, amount);

                    if (sourceIndex == -1)
                    {
                        return false;
                    }

                    var targetItems = targetInventory.GetItems();
                    //if we wont find a viable stack to merge into, 
                    //then we can have to take the first free slot
                    var targetIndex = targetItems.Count;
                    for (var i = 0; i < targetItems.Count - 1; i++)
                    {
                        if (new Item(targetItems[i]).itemType == desiredMaterial)
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                    return RawContainer.GetInventory(0).TransferItemTo(targetInventory, sourceIndex, targetIndex, true, (VRage.MyFixedPoint)amount);

                }
            }
        }

        public class Item
        {
            public enum ITEM
            {
                CONSTRUCTION_COMPONENT,
                COMPUTER_COMPONENTS,
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
                MOTOR,
                GIRDER,

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

            public Item(IMyInventoryItem rawItem)
            {

                _rawItem = rawItem;
                itemType = ConvertItemObjToItem(_rawItem);
                amount = _rawItem.Amount.ToIntSafe();
            }

            public ITEM itemType;

            public int amount;

            private IMyInventoryItem _rawItem;

            //function shamelessly stolen from http://thefinalfrontier.se/cargo-container-inventory/
            public static string DecodeItemName(IMyInventoryItem item)
            {

                var name = item.Content.SubtypeName;
                var typeId = item.Content.TypeId.ToString();


                if (name.Equals("Girder"))
                {
                    return "Girder";
                }

                if (name.Equals("Motor"))
                {
                    return "Motor";
                }

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
                return ConvertSubTypeAndTypeIdToItem(item.Content.SubtypeName, item.Content.TypeId.ToString());
            }

            public static ITEM ConvertSubTypeAndTypeIdToItem(string subTypeString, string typeId)
            {
                var name = subTypeString;
                //this identifier for construction components is different 
                //if the item is in the assembler queue or if it is in a container
                //idk why though
                if (name.Equals("Construction") || name.Equals("ConstructionComponent")) { return ITEM.CONSTRUCTION_COMPONENT; }
                if (name.Equals("MetalGrid")) { return ITEM.METALGRID; }
                if (name.Equals("InteriorPlate")) { return ITEM.INTERIOR_PLATE; }
                if (name.Equals("SteelPlate")) { return ITEM.STEEL_PLATE; }
                if (name.Equals("SmallTube")) { return ITEM.SMALL_STEEL_TUBE; }
                if (name.Equals("LargeTube")) { return ITEM.LARGE_STEEL_TUBE; }
                if (name.Equals("BulletproofGlass")) { return ITEM.BULLETPROOF_GLASS; }
                if (name.Equals("Reactor") || name.Equals("ReactorComponent")) { return ITEM.REACTOR_COMPONENT; }
                if (name.Equals("Thrust") || name.Equals("ThrustComponent")) { return ITEM.THRUSTER_COMPONENT; }
                if (name.Equals("ComputerComponent") || name.Equals("Computer")) { return ITEM.COMPUTER_COMPONENTS; }
                if (name.Equals("GravityGenerator") || name.Equals("GravityGeneratorComponent")) { return ITEM.GRAVGEN_COMPONENT; }
                if (name.Equals("DetectorComponent") || name.Equals("Detector")) { return ITEM.DETECTOR_COMPONENT; }
                if (name.Equals("RadioCommunicationComponent") || name.Equals("RadioCommunication")) { return ITEM.RADIO_COMPONENT; }
                if (name.Equals("MedicalComponent") || name.Equals("Medical")) { return ITEM.MEDICAL_COMPONENT; }
                if (name.Equals("Display")) { return ITEM.DISPLAY; }
                if (name.Equals("SolarCell")) { return ITEM.SOLAR_CELL; }
                if (name.Equals("PowerCell")) { return ITEM.POWER_CELL; }
                if (name.Equals("Motor")) { return ITEM.MOTOR; }

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
                Util.FatalError($"Unrecognized Item: Name = {name} TypeId = {typeId}");
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

            public static string ConvertItemTypeToString(ITEM item)
            {
                var ret = item.ToString();
                ret = ret.ToLower();
                ret = ret.Replace("_", " ");
                return ret;
            }

            public bool Equals(Item obj)
            {
                return obj.itemType == itemType;
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

            public IMyAssembler RawAssembler { get; }

            public string AssemblerName => RawAssembler.CustomName;

            public Reporter Reporter;

            private readonly Container resourcePool;

            public Assembler(IMyAssembler rawAssembler, Container resourcePool)
            {
                RawAssembler = rawAssembler;
                this.resourcePool = resourcePool;

                if (resourcePool == null)
                {
                    Util.FatalError("No resourceBox supplied on assembler creation");
                }

                if (RawAssembler == null)
                { Util.FatalError(ASSEMBLER_NOT_FOUND); }

                RawAssembler.UseConveyorSystem = false;
            }

            public bool MaterialMissing()
            {
                return !RawAssembler.IsProducing &&
                       !RawAssembler.IsQueueEmpty &&
                        RawAssembler.IsWorking;
            }

            public void CancelCurrentlyProducedItem()
            {
                if (RawAssembler.IsQueueEmpty)
                    return;
                var currentlyProducedItems = new List<MyProductionItem>();
                RawAssembler.GetQueue(currentlyProducedItems);
                RawAssembler.RemoveQueueItem(0, currentlyProducedItems[0].Amount);
            }

            public void MoveCurrentlyProducedItemToEndOfQueue()
            {
                if (RawAssembler.IsQueueEmpty)
                    return;
                var currentlyProducedItems = new List<MyProductionItem>();

                RawAssembler.GetQueue(currentlyProducedItems);
                var currentlyProducedItem = currentlyProducedItems[0];
                RawAssembler.RemoveQueueItem(0, currentlyProducedItems[0].Amount);
                RawAssembler.AddQueueItem(currentlyProducedItem.BlueprintId, currentlyProducedItem.Amount);
            }

            public bool EnqueueItem(Item.ITEM toBuild, long amount)
            {
                if (amount < 1)
                    return false;
                MyDefinitionId bluePrint;

                #region builditem switch
                switch (toBuild)
                {
                    case Item.ITEM.CONSTRUCTION_COMPONENT:
                        bluePrint = MyDefinitionId.Parse(ConstructionComponent);
                        break;
                    case Item.ITEM.COMPUTER_COMPONENTS:
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
                        Util.FatalError("Unknown Item to build given");
                        break;

                }
                #endregion

                return false;
            }

            public bool ObtainResourcesForItem(Item.ITEM toBuild, double amount)
            {
                if (amount < 1)
                {
                    Reporter?.ReportSoftError("amount error in resource aquisition");
                    return false;
                }

                var ret = true;
                #region builditem switch
                switch (toBuild)
                {
                    case Item.ITEM.CONSTRUCTION_COMPONENT:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 3.34);
                        break;
                    case Item.ITEM.COMPUTER_COMPONENTS:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 0.18);
                        ret &= ObtainResource(Item.ITEM.SILICON_WAFER, amount * 0.08);
                        break;
                    case Item.ITEM.DISPLAY:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 0.34);
                        ret &= ObtainResource(Item.ITEM.SILICON_WAFER, amount * 1.68);
                        break;
                    case Item.ITEM.METALGRID:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 4.01);
                        ret &= ObtainResource(Item.ITEM.NICKEL_INGOT, amount * 1.68);
                        ret &= ObtainResource(Item.ITEM.COBALT_INGOT, amount * 1.01);
                        break;
                    case Item.ITEM.INTERIOR_PLATE:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 1.18);
                        break;
                    case Item.ITEM.STEEL_PLATE:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 7.01);
                        break;
                    case Item.ITEM.SMALL_STEEL_TUBE:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 1.68);
                        break;
                    case Item.ITEM.LARGE_STEEL_TUBE:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 10.01);
                        break;
                    case Item.ITEM.BULLETPROOF_GLASS:
                        ret &= ObtainResource(Item.ITEM.SILICON_WAFER, amount * 5.01);
                        break;
                    case Item.ITEM.REACTOR_COMPONENT:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 5.01);
                        ret &= ObtainResource(Item.ITEM.GRAVEL, amount * 6.68);
                        ret &= ObtainResource(Item.ITEM.SILVER_INGOT, amount * 1.68);
                        break;
                    case Item.ITEM.THRUSTER_COMPONENT:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 10.1);
                        ret &= ObtainResource(Item.ITEM.COBALT_INGOT, amount * 3.34);
                        ret &= ObtainResource(Item.ITEM.GOLD_INGOT, amount * 0.34);
                        ret &= ObtainResource(Item.ITEM.PLATINUM_INGOT, amount * 0.14);
                        break;
                    case Item.ITEM.GRAVGEN_COMPONENT:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 200.1);
                        ret &= ObtainResource(Item.ITEM.COBALT_INGOT, amount * 73.34);
                        ret &= ObtainResource(Item.ITEM.GOLD_INGOT, amount * 3.34);
                        ret &= ObtainResource(Item.ITEM.SILVER_INGOT, amount * 1.68);
                        break;
                    case Item.ITEM.MEDICAL_COMPONENT:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 20.1);
                        ret &= ObtainResource(Item.ITEM.NICKEL_INGOT, amount * 23.34);
                        ret &= ObtainResource(Item.ITEM.SILVER_INGOT, amount * 6.68);
                        break;
                    case Item.ITEM.RADIO_COMPONENT:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 2.68);
                        ret &= ObtainResource(Item.ITEM.SILICON_WAFER, amount * 0.34);
                        break;
                    case Item.ITEM.DETECTOR_COMPONENT:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 1.68);
                        ret &= ObtainResource(Item.ITEM.NICKEL_INGOT, amount * 5.01);
                        break;
                    case Item.ITEM.SOLAR_CELL:
                        ret &= ObtainResource(Item.ITEM.NICKEL_INGOT, amount * 3.34);
                        ret &= ObtainResource(Item.ITEM.SILICON_WAFER, amount * 2.68);
                        break;
                    case Item.ITEM.POWER_CELL:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 3.34);
                        ret &= ObtainResource(Item.ITEM.SILICON_WAFER, amount * 0.34);
                        ret &= ObtainResource(Item.ITEM.NICKEL_INGOT, amount * 0.68);
                        break;
                    case Item.ITEM.MOTOR:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 6.68);
                        ret &= ObtainResource(Item.ITEM.NICKEL_INGOT, amount * 1.68);
                        break;
                    case Item.ITEM.GIRDER:
                        ret &= ObtainResource(Item.ITEM.IRON_INGOT, amount * 2.34);
                        break;
                    default:
                        throw new Exception("Invalid item for resource aquisition supplied");

                }
                #endregion

                return ret;
            }

            private bool ObtainResource(Item.ITEM resourceType, double amount)
            {
                if (resourcePool != null)
                {
                    resourcePool.MergeItemStacks();
                    if (!resourcePool.MoveResourcesTo(resourceType, amount, GetRawInputInventory()))
                    {
                        Reporter.ReportSoftError("Obtain Resources failed");
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    Reporter.ReportHardError("resourcePool was null on Resource aquisition");
                }
                return false;
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
                EnqueueItem(toBuild, quotaAmount - GetEnqueuedItemsOfType(toBuild));
            }

            /// <summary>
            /// Will search the Asemblers queue for the given itemType and returns the 
            /// </summary>
            /// <param name="itemType"></param>
            /// <returns></returns>
            public long GetEnqueuedItemsOfType(Item.ITEM itemType)
            {
                var queuedItems = new List<MyProductionItem>();
                RawAssembler.GetQueue(queuedItems);
                long retAmount = 0;

                foreach (var iterItem in queuedItems)
                {
                    if (Item.ConvertSubTypeAndTypeIdToItem(iterItem.BlueprintId.SubtypeName, iterItem.BlueprintId.SubtypeId.ToString()) == itemType)
                    {
                        retAmount += iterItem.Amount.ToIntSafe();
                    }
                }
                echodel.Invoke($"returning amount of {retAmount}");
                return retAmount;
            }

            private IMyInventory GetRawInventory(bool getRawOutPutInventory)
            {
                return RawAssembler.GetInventory(getRawOutPutInventory ? 1 : 0);
            }

            public IMyInventory GetRawInputInventory()
            {
                return GetRawInventory(false);
            }

            public IMyInventory GetRawOutputInventory()
            {
                return GetRawInventory(true);
            }

            private List<Item> GetInventory(bool getOutputInventory)
            {
                var ret = new List<Item>();
                foreach (var item in RawAssembler.GetInventory(getOutputInventory ? 1 : 0).GetItems())
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

            public bool EmptyOutputToInventory(IMyInventory targetInventory)
            {
                if (targetInventory == null
                    || targetInventory.IsFull)
                { return false; }

                var outPutInv = GetRawOutputInventory();
                while (outPutInv.IsItemAt(0))
                {
                    if (!outPutInv.TransferItemTo(targetInventory, 0))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        #endregion

        public class Util
        {
            public static void FatalError(string errorMessage)
            {
                throw new Exception(errorMessage);
            }

            public static int CountItemInInventory(List<Item> toCountIn, Item.ITEM toCount)
            {
                var itemCount = 0;
                foreach (var item in toCountIn)
                {
                    if (item.Equals(toCount))
                    {
                        itemCount += item.amount;
                    }
                }
                return itemCount;
            }

            public class FixedSizeQueue<T> : Queue<T>
            {
                private readonly object _syncObj = new object();
                public int FixedCapacity { get; }
                public FixedSizeQueue(int fixedCapacity)
                {
                    FixedCapacity = fixedCapacity;
                }

                public new T Dequeue()
                {
                    lock (_syncObj)
                    {
                        return base.Dequeue();
                    }
                }

                public new void Enqueue(T obj)
                {
                    lock (_syncObj)
                    {
                        while (Count >= FixedCapacity)
                        {
                            Dequeue();
                        }
                        base.Enqueue(obj);
                    }
                }
            }

            public static int DeltaTime(DateTime last)
            {
                DateTime now = DateTime.Now;
                TimeSpan delta = now.Subtract(last);
                return (int)delta.TotalSeconds;
            }
        }


        #region Reporter

        public abstract class Reporter
        {
            private const string ShortInfoTag = "I";
            private const string ShortWarnTag = "W";
            private const string ShortSoftErrorTag = "SE";
            private const string ShortHardErrorTag = "HE";

            private const string VerboseInfoTag = "Info";
            private const string VerboseWarnTag = "Warning";
            private const string VerboseSoftErrorTag = "Softerror";
            private const string VerboseHardErrorTag = "HardError";

            protected string InfoTag => UseVerboseTags ? VerboseInfoTag : ShortInfoTag;
            protected string WarnTag => UseVerboseTags ? VerboseWarnTag : ShortWarnTag;
            protected string SoftErrorTag => UseVerboseTags ? VerboseSoftErrorTag : ShortSoftErrorTag;
            protected string HardErrorTag => UseVerboseTags ? VerboseHardErrorTag : ShortHardErrorTag;

            public bool UseVerboseTags;

            public abstract void ReportInfo(string message);
            public abstract void ReportWarning(string message);
            public abstract void ReportSoftError(string message);
            public abstract void ReportHardError(string message);
        }

        public class EchoReporter : Reporter
        {
            private readonly Action<string> _echoDelegate;

            public EchoReporter(Action<string> echoDelegate)
            {
                if (echoDelegate == null)
                {
                    Util.FatalError("Supplied null action to EchoReporter");
                }
                _echoDelegate = echoDelegate;
            }

            public override void ReportInfo(string message)
            {
                _echoDelegate.Invoke(InfoTag + $":{message}");
            }

            public override void ReportWarning(string message)
            {
                _echoDelegate.Invoke(WarnTag + $":{message}");
            }

            public override void ReportSoftError(string message)
            {
                _echoDelegate.Invoke(SoftErrorTag + $":{message}");
            }

            public override void ReportHardError(string message)
            {
                _echoDelegate.Invoke(HardErrorTag + $":{message}");
            }
        }

        public class RollingLcdReporter : Reporter
        {

            private object sync = new object();
            private IMyTextPanel _lcdOutPanel;

            private const float FontSize = 0.89f;
            private const int LineLimit = 20;

            private Util.FixedSizeQueue<string> rollingLog;

            public RollingLcdReporter(IMyTextPanel outPutPanel)
            {
                if (outPutPanel == null)
                {
                    Util.FatalError("Supplied null object to LCDReporter");
                }

                _lcdOutPanel = outPutPanel;
                _lcdOutPanel.ShowPublicTextOnScreen();
                _lcdOutPanel.FontSize = FontSize;
                rollingLog = new Util.FixedSizeQueue<string>(LineLimit);

            }

            private void PrintRollingLog()
            {
                StringBuilder text = new StringBuilder();

                foreach (var logEntry in rollingLog)
                {
                    text.Append(logEntry + "\n");
                }

                _lcdOutPanel.WritePublicText(text);
            }

            public override void ReportInfo(string message)
            {
                lock (sync)
                {
                    rollingLog.Enqueue($"{InfoTag}:{message}");
                    PrintRollingLog();
                }
            }

            public override void ReportWarning(string message)
            {
                lock (sync)
                {
                    rollingLog.Enqueue($"{WarnTag}:{message}");
                    PrintRollingLog();
                }
            }

            public override void ReportSoftError(string message)
            {
                lock (sync)
                {
                    rollingLog.Enqueue($"{SoftErrorTag}:{message}");
                    PrintRollingLog();
                }
            }

            public override void ReportHardError(string message)
            {
                lock (sync)
                {
                    rollingLog.Enqueue($"{HardErrorTag}:{message}");
                    PrintRollingLog();
                }
            }
        }

        #endregion


        #endregion
    }
}