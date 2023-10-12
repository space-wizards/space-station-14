using System.Linq;

namespace Content.Shared.Ganimed.SponsorManager;

public sealed class SponsorManager
{
    private Dictionary<string, DateTime> sponsors =
		new Dictionary<string, DateTime>(){
			{"Laastrog", new DateTime(2023, 08, 15)},
			{"Swaiper5", new DateTime(2023, 09, 26)},
			{"Mexanik78", new DateTime(2023, 13, 10)},
			{"localhost@JoeGenero", new DateTime(2023, 10, 07)}
		};

    public bool IsSponsor(String? nickname)
    {
        if (nickname is null)
			return false;
		
		if (!sponsors.TryGetValue(nickname, out var sponsorTime))
			return false;
		
		return IsStillSponsor(sponsorTime);
    }
	
	public bool IsStillSponsor(DateTime sponsorDate)
	{
		return (sponsorDate.Add(TimeSpan.Parse("30:00:00:00")) > DateTime.Now) 
			&& (sponsorDate <= DateTime.Now);
	}
}
