from pathlib import Path
root=Path(__file__).resolve().parents[1]/'Assets/Workbench/Runtime'
changes={
 'GameRoot.cs':[
  ('SetActive(selectedLevel<9)','SetActive(won&&selectedLevel<9)'),
  ('" / 5");Obj("Result/Next")','" / 5\\nЗвёзды копятся между попытками.");Obj("Result/Next")'),
  ('Щит: ПКМ отражает снаряды в сторону прицела 3 секунды.','Щит: ПКМ создаёт защиту со стороны прицела на 3 секунды.'),
 ],
 'UiFactory.cs':[
  ('"ПАУЗА",1660,16,240,64','"ПАУЗА",1660,996,240,64'),
  ('"EnemyPanel",16,112,380,80','"EnemyPanel",1524,16,380,80'),
  ('"EnemyHP","",40,128,330,48','"EnemyHP","",1548,30,330,48'),
 ],
 'WorldView.cs':[
  ('p.type==5?"rifle":','p.type==5?"RifleDirections/dir_"+GameData.Direction(p.velocity).ToString("00"):'),
  ('v.body.transform.rotation=Quaternion.Euler','v.body.transform.rotation=p.type==5?Quaternion.identity:Quaternion.Euler'),
 ]
}
for file,replacements in changes.items():
    p=root/file;text=p.read_text(encoding='utf-8')
    for old,new in replacements:
        assert text.count(old)==1,(file,old,text.count(old))
        text=text.replace(old,new)
    p.write_text(text,encoding='utf-8')
