namespace Example.SNRReduction
{
    public class AppConfig
    {
        public string CardName { get; set; } = "";
        public int BaselineSeconds { get; set; } = 3;
        public int SignalSeconds { get; set; } = 3;
        public int TestToneHz { get; set; } = 1000;
        public int Dbfs { get; set; } = -10;
        public string ResultsFile { get; set; } = "snr_results.json";
    }
}
