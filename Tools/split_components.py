from pathlib import Path
base=Path('Assets/Workbench/Runtime')
p=base/'ScreenMotion.cs';s=p.read_text(encoding='utf-8');popup=s.index('public sealed class PopupMotion:');health=s.index('public sealed class HealthBar:');header='using System;\nusing System.Collections.Generic;\nusing UnityEngine;\nusing UnityEngine.UI;\nnamespace Workbench {\n'
(base/'PopupMotion.cs').write_text(header+s[popup:health]+'}\n',encoding='utf-8')
(base/'HealthBar.cs').write_text(header+s[health:],encoding='utf-8');p.write_text(s[:popup]+'}\n',encoding='utf-8')
p=base/'PixelEffects.cs';s=p.read_text(encoding='utf-8');i=s.index('public sealed class UiParticles:');(base/'UiParticles.cs').write_text(header+s[i:],encoding='utf-8');p.write_text(s[:i]+'}\n',encoding='utf-8')
