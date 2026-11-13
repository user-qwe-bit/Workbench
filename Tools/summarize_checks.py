from pathlib import Path
import csv,json,statistics
root=Path(__file__).resolve().parents[1]
rows=list(csv.DictReader((root/'TestResults/campaign-simulation.csv').open(encoding='utf-8')))
summary={'matches':len(rows),'timed_out':sum(int(r['winner'])==-2 for r in rows),'duration_seconds':{'min':min(float(r['seconds']) for r in rows),'median':statistics.median(float(r['seconds']) for r in rows),'max':max(float(r['seconds']) for r in rows)},'by_level':[]}
for level in range(1,11):
    subset=[r for r in rows if int(r['level'])==level]
    summary['by_level'].append({'level':level,'compact_wins':sum(r['design']=='compact' and r['winner']=='0' for r in subset),'heavy_wins':sum(r['design']=='heavy' and r['winner']=='0' for r in subset),'arenas_with_tested_player_win':sorted({int(r['arena']) for r in subset if r['winner']=='0'})})
summary['limitations']='Two automated pilot builds per level/arena, one seed. This is deterministic model simulation, not Windows player validation or proof of all star goals.'
(root/'TestResults/campaign-summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print(json.dumps(summary,indent=2))
