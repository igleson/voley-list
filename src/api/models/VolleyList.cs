namespace api.models;

public record VolleyListing
{
    public string Name { get; init; }

    public int Capacity { get; set; } = 18;

    public List<Player> Players { get; set; } = [];

    public List<Player> Reserve { get; set; } = [];

    public bool Promoted { get; set; } = false;
}

public record Player(string name, string? inviter)
{
    public bool isInvitee()
    {
        return string.IsNullOrEmpty(inviter);
    }
}