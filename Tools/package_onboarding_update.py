from pathlib import Path
import datetime
import hashlib
import json
import shutil
import zipfile

root = Path(__file__).resolve().parents[1]
build = root / 'Builds/Windows'
results = root / 'TestResults'
now = datetime.datetime.now(datetime.timezone.utc).isoformat()

# This delivery step packages the already built player. It does not run tests.
for path in [root/'Assets/Workbench/Editor/ProjectBuilder.cs',
             root/'Tools/finalize_release.py', root/'Tools/document_driving_release.py',
             root/'README_RU.md', root/'DEVELOPMENT_STATUS.md', *sorted((root/'Docs').glob('*.md'))]:
    text = path.read_text(encoding='utf-8').replace('Windows-Driving', 'Windows')
    if path.name in ('DEVELOPMENT_STATUS.md', 'POLISH_RU.md'):
        text = text.split('\n## ???', 1)[0].rstrip() + '\n'
    if path.name == 'finalize_release.py':
        text = '\n'.join(line for line in text.splitlines() if not ('??????' in line and line.startswith(('readme+=', 'status+=')))) + '\n'
    path.write_text(text, encoding='utf-8')

p = root/'Tools/verify_input_copy.py'
text = p.read_text(encoding='utf-8')
text = text.replace("old=r/'Builds/Windows/", "old=r/'TestResults/BuildHistory/Windows-BeforeOnboarding/")
p.write_text(text.replace('Windows-Driving', 'Windows'), encoding='utf-8')

onboarding = ('При каждом запуске открывается предыстория; её можно пропустить. '
              'Обучение в конструкторе повторяется до первой победы в текущем сеансе, '
              'а пояснения специальных модулей — один раз за сеанс для каждого типа. '
              'Старые флаги завершения не отключают обучение при новом запуске. '
              'Звёзды, сборки и настройки сохраняются.')
hud = ('В HUD оставлен один крупный таймер прошедшего времени. Звезда и время цели убраны; '
       'условие второй звезды по-прежнему указано в заданиях уровня. Движение роботов не менялось.')

p = root/'README_RU.md'
p.write_text(p.read_text(encoding='utf-8') + '\n## Последнее обновление\n\n' + hud + '\n\n' + onboarding + '\n', encoding='utf-8')
p = root/'Docs/POLISH_RU.md'
text = p.read_text(encoding='utf-8').replace('Таймер боя и целевое время со звездой раздельны. Истечение целевого времени только меняет вид звезды.', hud)
p.write_text(text + '\n## Предыстория и обучение при запуске\n\n' + onboarding + '\n', encoding='utf-8')

smoke_path = build/'TestResults/Runtime/smoke.txt'
smoke = smoke_path.read_text(encoding='utf-8')
if 'STOPPED_BY_USER' not in smoke:
    smoke += '\nSTOPPED_BY_USER ' + now + '\n'
    smoke_path.write_text(smoke, encoding='utf-8')
partial_checks = smoke.count('PASS ')
note = (f'Обновление {now}: Windows Player пересобран без ошибок. '
        f'Начатый прогон остановлен по просьбе пользователя; до остановки выполнено {partial_checks} проверок. '
        'Полный прогон этой версии и дополнительная визуальная проверка таймера не заявляются. '
        'Последующие тесты не запускались. Сборка перенесена в `Builds/Windows/Workbench.exe`; '
        'папки `Windows-Driving` больше нет. Следующие сборки также создаются в `Builds/Windows`.')
p = root/'Docs/VERIFICATION_RU.md'
p.write_text('# Последнее обновление\n\n' + note + '\n\n' + hud + '\n\n' + onboarding +
             '\n\nНиже сохранён отчёт предыдущей полной проверки; его результаты не выдаются за новый полный прогон.\n\n' + p.read_text(encoding='utf-8'), encoding='utf-8')
p = root/'DEVELOPMENT_STATUS.md'
p.write_text(p.read_text(encoding='utf-8') + '\n## Последнее обновление: таймер, обучение и папка сборки\n\n' +
             hud + '\n\n' + onboarding + '\n\n' + note + '\n', encoding='utf-8')

p = root/'Tools/finalize_release.py'
text = p.read_text(encoding='utf-8')
text = text.replace("(root/'README_RU.md').write_text(readme,encoding='utf-8')",
                    'readme+=' + repr('\n' + hud + '\n\n' + onboarding + '\n') + '\n' +
                    "(root/'README_RU.md').write_text(readme,encoding='utf-8')")
text = text.replace("(root/'DEVELOPMENT_STATUS.md').write_text(status,encoding='utf-8')",
                    'status+=' + repr('\n## Таймер и обучение\n\n' + hud + '\n\n' + onboarding + '\n') + '\n' +
                    "(root/'DEVELOPMENT_STATUS.md').write_text(status,encoding='utf-8')")
p.write_text(text, encoding='utf-8')

for name in ['README_RU.md', 'THIRD_PARTY_NOTICES.md']:
    shutil.copy2(root/name, build/name)
shutil.copytree(root/'Docs', build/'Docs', dirs_exist_ok=True)
manifest_path = build/'BUILD_MANIFEST.json'
previous = json.loads(manifest_path.read_text(encoding='utf-8'))
(results/'BuildHistory/manifest-before-onboarding.json').write_text(json.dumps(previous, indent=2), encoding='utf-8')
manifest = {
    'built_utc': (results/'editor-request-result.txt').read_text().split()[-1],
    'packaged_utc': now, 'unity': '6000.6.6f1', 'platform': 'Windows x64 Mono',
    'build_directory': 'Builds/Windows',
    'game_data_sha256': previous['game_data_sha256'],
    'assembly_sha256': hashlib.sha256((build/'Workbench_Data/Managed/Assembly-CSharp.dll').read_bytes()).hexdigest(),
    'critical_checks_during_build': 53,
    'player_validation': 'Stopped at user request; no additional testing',
    'player_checks_completed_before_stop': partial_checks,
    'previous_full_validation': {key: previous[key] for key in ('built_utc','player_checks','reset_checks','campaign_battles','star_conditions')},
    'manual_camera_check': 'User will perform', 'live_human_voice_check': False
}
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')

archive = root/'Builds/Workbench-Windows-x64.zip'
temporary = archive.with_suffix('.zip.tmp')
with zipfile.ZipFile(temporary, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for file in sorted(build.rglob('*')):
        if not file.is_file():
            continue
        rel = file.relative_to(build)
        if any('BackUpThisFolder' in part or part in ('TestResults','__pycache__') for part in rel.parts):
            continue
        z.write(file, Path('Workbench')/rel)
temporary.replace(archive)
(results/'release-package.json').write_text(json.dumps({
    'zip': str(archive.relative_to(root)), 'bytes': archive.stat().st_size,
    'sha256': hashlib.sha256(archive.read_bytes()).hexdigest(),
    'additional_verification': 'Not run at user request'
}, indent=2), encoding='utf-8')
print('Packaged Builds/Windows and Workbench-Windows-x64.zip. No tests launched.')
