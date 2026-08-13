import wave
import struct
import math
import os

def create_wav(filename, samples, sample_rate=44100):
    with wave.open(filename, 'w') as wav_file:
        wav_file.setnchannels(1)
        wav_file.setsampwidth(2)
        wav_file.setframerate(sample_rate)
        for sample in samples:
            # Clamp to 16-bit range
            clamped = max(-32768, min(32767, int(sample * 32767)))
            wav_file.writeframes(struct.pack('h', clamped))

def generate_generator_sound():
    print("Generating generator_complete.wav...")
    samples = []
    sr = 44100
    duration = 2.0
    for i in range(int(sr * duration)):
        t = i / sr
        # Frequency sweeps up from 50Hz to 300Hz
        freq = 50 + (250 * t / duration)
        # Add some mechanical grit
        wave1 = math.sin(2 * math.pi * freq * t)
        wave2 = math.sin(2 * math.pi * (freq * 1.5) * t) * 0.5
        env = min(1.0, t * 2) * max(0.0, 1.0 - (t / duration)**2)
        samples.append((wave1 + wave2) * 0.4 * env)
    create_wav("Assets/Audio/generator_complete.wav", samples, sr)

def generate_mission_complete():
    print("Generating mission_complete.wav...")
    samples = []
    sr = 44100
    duration = 1.5
    for i in range(int(sr * duration)):
        t = i / sr
        # A pleasant bell chime (e.g., C6)
        freq = 1046.50
        wave1 = math.sin(2 * math.pi * freq * t)
        wave2 = math.sin(2 * math.pi * (freq * 1.5) * t) * 0.3
        env = math.exp(-t * 3) # fast decay
        samples.append((wave1 + wave2) * 0.5 * env)
    create_wav("Assets/Audio/mission_complete.wav", samples, sr)

def generate_museum_open():
    print("Generating museum_open.wav...")
    samples = []
    sr = 44100
    duration = 3.0
    # C Major chord (C, E, G, C)
    freqs = [523.25, 659.25, 783.99, 1046.50]
    for i in range(int(sr * duration)):
        t = i / sr
        sample = sum(math.sin(2 * math.pi * f * t) for f in freqs) / len(freqs)
        env = min(1.0, t * 5) * max(0.0, 1.0 - (t / duration))
        samples.append(sample * 0.5 * env)
    create_wav("Assets/Audio/museum_open.wav", samples, sr)

def generate_victory():
    print("Generating victory.wav...")
    samples = []
    sr = 44100
    duration = 2.5
    # C Major Arpeggio (C, E, G, C) fast
    notes = [523.25, 659.25, 783.99, 1046.50]
    for i in range(int(sr * duration)):
        t = i / sr
        note_idx = min(len(notes)-1, int(t / 0.2))
        freq = notes[note_idx]
        sample = math.sin(2 * math.pi * freq * t)
        env = max(0.0, 1.0 - ((t % 0.2) / 0.2)) if t < 0.8 else max(0.0, 1.0 - (t-0.8)/1.7)
        samples.append(sample * 0.4 * env)
    create_wav("Assets/Audio/victory.wav", samples, sr)

def generate_bgm():
    print("Generating bgm.wav...")
    samples = []
    sr = 44100
    duration = 10.0
    for i in range(int(sr * duration)):
        t = i / sr
        # Low ambient drone
        freq1 = 130.81 # C3
        freq2 = 196.00 # G3
        wave1 = math.sin(2 * math.pi * freq1 * t + math.sin(t) * 0.5)
        wave2 = math.sin(2 * math.pi * freq2 * t + math.cos(t) * 0.5)
        # Slow pulse envelope
        env = 0.5 + 0.3 * math.sin(2 * math.pi * 0.2 * t)
        samples.append((wave1 + wave2) * 0.15 * env)
    create_wav("Assets/Audio/bgm.wav", samples, sr)

if __name__ == "__main__":
    if not os.path.exists("Assets/Audio"):
        os.makedirs("Assets/Audio")
    
    generate_generator_sound()
    generate_mission_complete()
    generate_museum_open()
    generate_victory()
    generate_bgm()
    print("All sounds generated successfully!")
