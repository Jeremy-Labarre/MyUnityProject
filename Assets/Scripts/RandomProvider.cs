public static class RandomProvider
{
    public static double GetDouble(int seed, int slice, int segment)
    {
        int combined = seed + slice * 1000 + segment;
        System.Random rand = new System.Random(combined);
        return rand.NextDouble();
    }
}
