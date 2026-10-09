namespace MelavoClient;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        if(args.Any(a=>a.EndsWith("checks",StringComparison.Ordinal)))Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        using var mutex=new Mutex(true,args.Any(a=>a is "--web-checks" or "--preview" or "--ui-checks" or "--validate" or "--network-check" or "--update-checks" or "--hardening-checks" or "--icmp-checks" or "--latency-checks" or "--render-checks" or "--profile-checks" or "--management-checks")?"Local\\MelavoReview-"+Guid.NewGuid():"Local\\"+SubscriptionStore.ProfileName,out var unique);
        if(!unique){MessageBox.Show("برنامه از قبل باز است.");return;}
        if(args.Length==2 && args[0]=="--management-checks"){Environment.Exit(Client.ManagementChecks(args[1]));return;}
        if(args.Length>1 && args[0]=="--subscription-checks"){Environment.Exit(Client.SubscriptionChecks(args.Skip(1).ToArray()).GetAwaiter().GetResult());return;}
        if(args.Length==2 && args[0]=="--validate") { Environment.Exit(Client.Validate(args[1])); return; }
        if(args.Length==2 && args[0]=="--network-check") { Environment.Exit(Client.NetworkCheck(args[1]).GetAwaiter().GetResult()); return; }
        if(args.Length==2 && args[0]=="--latency-checks"){Environment.Exit(Client.LatencyChecks(args[1]).GetAwaiter().GetResult());return;}
        if(args.Length==2&&args[0]=="--hardening-checks"){Environment.Exit(Client.HardeningChecks(args[1]).GetAwaiter().GetResult());return;}
        if(args.Contains("--icmp-checks")){Environment.Exit(Client.IcmpChecks().GetAwaiter().GetResult());return;}
        if(args.Contains("--profile-checks")){SubscriptionStore.SelfTest();var count=SubscriptionStore.Read().Count;File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"profile-checks.txt"),$"Edition: {SubscriptionStore.Edition}; profile: {SubscriptionStore.ProfileName}; saved groups: {count}; encrypted roundtrip: PASS");return;}
        if(args.Contains("--render-checks")){Environment.Exit(Client.RenderChecks());return;}
        if(args.Contains("--ui-checks")){Environment.Exit(Client.UiChecks());return;}
        if(args.Contains("--update-checks")){Environment.Exit(UpdateChecks.Run());return;}
        if(args.Contains("--web-checks")){Environment.Exit(Client.WebChecks());return;}
        Application.Run(new Client(args.Contains("--preview"),true));
    }    
}
