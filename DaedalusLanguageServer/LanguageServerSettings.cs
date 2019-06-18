namespace DaedalusLanguageServer
{
    public partial class LanguageServerSession
    {
        public class LanguageServerSettings
        {
            public int MaxNumberOfProblems { get; set; } = 10;

            public LanguageServerTraceSettings Trace { get; } = new LanguageServerTraceSettings();
        }
    }

    public partial class LanguageServerTraceSettings
    {
        public string Server { get; set; }
    }
}


