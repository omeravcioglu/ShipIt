import shutil

src = "soundtrack.py"
shutil.copy(src, "work60/soundtrack_lofi_backup.py")
s = open(src, encoding="utf8").read()
a = s.index("# ------------------------------------------------------------------------------------------------ the song")
b = s.index("# ------------------------------------------------------------------------------------------- sound effects")
song = open("work60/song_upbeat.py.txt", encoding="utf8").read()
s = s[:a] + song + s[b:]
s = s.replace("BPM = 96", "BPM = 120", 1)
s = s.replace("Music: a cozy lo-fi groove in F major at 96 BPM: FM electric piano, kalimba melody, round sine bass, brushed\ndrums with swing, a warm pad and a little vinyl crackle, with convolution reverb.",
              "Music: a sunny, upbeat groove in F major at 120 BPM (I-V-vi-IV): FM electric piano stabs, a kalimba melody doubled\nby a quiet glockenspiel, a bouncing octave bass, four-on-the-floor kick with claps, hats and tambourine, a warm pad,\nwith convolution reverb.")
open(src, "w", encoding="utf8").write(s)
print("ok", "BPM = 120" in s)
