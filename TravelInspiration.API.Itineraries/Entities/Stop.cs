﻿namespace TravelInspiration.API.Itineraries.Entities;

public sealed class Stop(string name) : AuditableEntity 
{
    public int Id { get; set; }
    public string Name { get; set; } = name;
    public Uri? ImageUri { get; set; }
    public int ItineraryId { get; set; }
    public bool? Suggested { get; set; }
    public Itinerary? Itinerary { get; set; }  
}
