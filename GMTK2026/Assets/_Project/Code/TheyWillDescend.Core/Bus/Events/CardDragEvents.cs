namespace TheyWillDescend.Core.Bus.Events
{
    /// <summary>Player started dragging a resource card from a tray.</summary>
    public readonly struct CardDragStartedEvent
    {
        public readonly string ResourceId;

        public CardDragStartedEvent(string resourceId)
        {
            ResourceId = resourceId ?? string.Empty;
        }
    }

    /// <summary>Player successfully dropped a resource card onto a production building.</summary>
    public readonly struct CardDroppedOnBuildingEvent
    {
        public readonly int BuildingId;
        public readonly string ResourceId;

        public CardDroppedOnBuildingEvent(int buildingId, string resourceId)
        {
            BuildingId = buildingId;
            ResourceId = resourceId ?? string.Empty;
        }
    }
}
