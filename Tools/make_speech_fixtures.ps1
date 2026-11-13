$fixtureDir = Join-Path $PSScriptRoot '../TestResults/InputFixtures'
New-Item -ItemType Directory -Force -Path $fixtureDir | Out-Null
$speaker = New-Object -ComObject SAPI.SpVoice
$voice = $speaker.GetVoices() | Where-Object { $_.GetDescription() -like '*Irina*' } | Select-Object -First 1
if ($null -eq $voice) { throw 'Russian SAPI voice unavailable' }
$speaker.Voice = $voice
$commands = @('начать бой','пауза','продолжить')
for ($index = 0; $index -lt $commands.Count; $index++) {
    $stream = New-Object -ComObject SAPI.SpFileStream
    $stream.Format.Type = 18
    $stream.Open((Join-Path $fixtureDir "command_$index.wav"),3,$false)
    $speaker.AudioOutputStream = $stream
    $speaker.Speak($commands[$index]) | Out-Null
    $stream.Close()
}
