namespace TheyWillDescend.Core.Bus.Events
{
    public readonly struct BuildingUnlockedEvent
    {
        public readonly int BuildingId;

        public BuildingUnlockedEvent(int buildingId)
        {
            BuildingId = buildingId;
        }
    }

    public readonly struct BuildingBuildProgressEvent
    {
        public readonly int BuildingId;
        public readonly string ResourceId;
        public readonly int Stored;
        public readonly int Required;

        public BuildingBuildProgressEvent(int buildingId, string resourceId, int stored, int required)
        {
            BuildingId = buildingId;
            ResourceId = resourceId;
            Stored = stored;
            Required = required;
        }
    }

    public readonly struct BuildingConstructionStartedEvent
    {
        public readonly int BuildingId;

        public BuildingConstructionStartedEvent(int buildingId)
        {
            BuildingId = buildingId;
        }
    }

    public readonly struct BuildingConstructedEvent
    {
        public readonly int BuildingId;

        public BuildingConstructedEvent(int buildingId)
        {
            BuildingId = buildingId;
        }
    }
}
