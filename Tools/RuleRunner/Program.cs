using System;
using System.IO;
using System.Text.Json;
using System.Linq;
using Workbench;
var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.InvariantCulture;
var data=JsonSerializer.Deserialize<GameData>(File.ReadAllText(Path.Combine(root,"Assets/Workbench/Resources/GameData.json")),new JsonSerializerOptions{IncludeFields=true});
try {var passed=RuleChecks.Run(data);var report=string.Join("\n",passed.ConvertAll(s=>"PASS "+s));Console.WriteLine(report);File.WriteAllText(Path.Combine(root,"TestResults/dotnet-rule-tests.txt"),report);}
catch(Exception e){Console.Error.WriteLine(e);File.WriteAllText(Path.Combine(root,"TestResults/dotnet-rule-tests.txt"),"FAIL "+e);Environment.ExitCode=1;}
if(args.Contains("--campaign")&&Environment.ExitCode==0)CampaignChecks.Run(data,Path.Combine(root,"TestResults/campaign-simulation.csv"));
if(args.Contains("--goals")&&Environment.ExitCode==0)GoalChecks.Run(data,Path.Combine(root,"TestResults"));
if(args.Contains("--focused-goals")&&Environment.ExitCode==0)GoalChecks.Run(data,Path.Combine(root,"TestResults"),true);
if(args.Contains("--final-goals")&&Environment.ExitCode==0)GoalChecks.Run(data,Path.Combine(root,"TestResults"),true,true);
if(args.Contains("--evade-goals")&&Environment.ExitCode==0)EvasiveChecks.Run(data,Path.Combine(root,"TestResults"));
if(args.Contains("--target-checks")&&Environment.ExitCode==0){try{TargetChecks.Run(data,Path.Combine(root,"TestResults/target-checks.txt"));}catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}}
if(args.Contains("--balance-probe")&&Environment.ExitCode==0)BalanceProbe.Run(data,Path.Combine(root,"TestResults"));
if(args.Contains("--balanced-goals")&&Environment.ExitCode==0)BalancedGoals.Run(data,Path.Combine(root,"TestResults"));
if(args.Contains("--balanced-missing")&&Environment.ExitCode==0)BalancedGoals.Run(data,Path.Combine(root,"TestResults"),true);
