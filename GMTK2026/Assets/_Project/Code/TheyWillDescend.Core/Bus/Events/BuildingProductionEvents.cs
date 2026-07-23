namespace TheyWillDescend.Core.Bus.Events
{
    public readonly struct BuildingWorkersChangedEvent
    {
        public readonly int BuildingId;
        public readonly int Workers;

        public BuildingWorkersChangedEvent(int buildingId, int workers)
        {
            BuildingId = buildingId;
            Workers = workers;
        }
    }

    public readonly struct BuildingInputChangedEvent
    {
        public readonly int BuildingId;
        public readonly string ResourceId;
        public readonly int Stored;
        public readonly int Required;

        public BuildingInputChangedEvent(int buildingId, string resourceId, int stored, int required)
        {
            BuildingId = buildingId;
            ResourceId = resourceId;
            Stored = stored;
            Required = required;
        }
    }

    public readonly struct BuildingProductionProgressEvent
    {
        public readonly int BuildingId;
        public readonly float NormalizedProgress;

        public BuildingProductionProgressEvent(int buildingId, float normalizedProgress)
        {
            BuildingId = buildingId;
            NormalizedProgress = normalizedProgress;
        }
    }

    public readonly struct ResourceProducedEvent
    {
        public readonly int BuildingId;
        public readonly string ResourceId;

        public ResourceProducedEvent(int buildingId, string resourceId)
        {
            BuildingId = buildingId;
            ResourceId = resourceId;
        }
    }
}
