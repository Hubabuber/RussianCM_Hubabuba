namespace Content.Server._RuCM.Kaban;

// CMU14: exact cumulative-weight sampling, without replacement at the existing caller's slot loop.
public static class KabanLottery
{
    public static int Select(IReadOnlyList<int> weights, int ticket)
    {
        if (ticket < 0)
            throw new ArgumentOutOfRangeException(nameof(ticket));
        for (var i = 0; i < weights.Count; i++)
        {
            if (weights[i] <= 0)
                throw new ArgumentOutOfRangeException(nameof(weights));
            if (ticket < weights[i])
                return i;
            ticket -= weights[i];
        }
        throw new ArgumentOutOfRangeException(nameof(ticket));
    }
}
