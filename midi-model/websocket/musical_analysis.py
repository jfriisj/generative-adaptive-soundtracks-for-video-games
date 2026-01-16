#!/usr/bin/env python3
"""
Advanced Musical Analysis Module
Provides harmonic, rhythmic, and structural analysis for RQ2
"""
import numpy as np
from collections import Counter, defaultdict
from typing import List, Dict, Tuple, Optional
import sys
from pathlib import Path

# Add src to path
sys.path.insert(0, str(Path(__file__).parent.parent / "src"))
import MIDI


class KeyDetector:
    """Detect musical key using Krumhansl-Schmuckler algorithm"""
    
    # Major and minor key profiles (correlation weights)
    MAJOR_PROFILE = np.array([6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88])
    MINOR_PROFILE = np.array([6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17])
    
    KEY_NAMES = ['C', 'C#', 'D', 'D#', 'E', 'F', 'F#', 'G', 'G#', 'A', 'A#', 'B']
    
    @classmethod
    def detect_key(cls, notes: List[Tuple[int, int, int]]) -> Tuple[str, float]:
        """
        Detect key from list of (time, pitch, velocity) tuples
        Returns (key_name, confidence)
        """
        if not notes:
            return ("C", 0.0)
        
        # Count pitch class occurrences (weighted by duration/velocity)
        pitch_class_weights = np.zeros(12)
        for time, pitch, velocity in notes:
            pitch_class = pitch % 12
            pitch_class_weights[pitch_class] += velocity
        
        # Normalize
        if pitch_class_weights.sum() > 0:
            pitch_class_weights /= pitch_class_weights.sum()
        
        # Correlate with all 24 keys (12 major + 12 minor)
        best_correlation = -1
        best_key = "C"
        best_mode = "major"
        
        for tonic in range(12):
            # Major key
            rotated_profile = np.roll(cls.MAJOR_PROFILE, tonic)
            correlation = np.corrcoef(pitch_class_weights, rotated_profile)[0, 1]
            if correlation > best_correlation:
                best_correlation = correlation
                best_key = cls.KEY_NAMES[tonic]
                best_mode = "major"
            
            # Minor key
            rotated_profile = np.roll(cls.MINOR_PROFILE, tonic)
            correlation = np.corrcoef(pitch_class_weights, rotated_profile)[0, 1]
            if correlation > best_correlation:
                best_correlation = correlation
                best_key = cls.KEY_NAMES[tonic] + "m"
                best_mode = "minor"
        
        confidence = (best_correlation + 1) / 2  # Map [-1, 1] to [0, 1]
        return (best_key, confidence)
    
    @classmethod
    def analyze_key_stability(cls, notes: List[Tuple[int, int, int]], 
                             window_size: int = 20) -> Dict:
        """
        Analyze key stability over time using sliding window
        Returns metrics about key changes and stability
        """
        if len(notes) < window_size:
            key, confidence = cls.detect_key(notes)
            return {
                "primary_key": key,
                "key_changes": 0,
                "stability_score": confidence,
                "key_distribution": {key: 1.0}
            }
        
        # Sliding window analysis
        keys_detected = []
        for i in range(0, len(notes) - window_size, window_size // 2):
            window = notes[i:i + window_size]
            key, confidence = cls.detect_key(window)
            keys_detected.append(key)
        
        # Count key changes
        key_changes = sum(1 for i in range(len(keys_detected) - 1) 
                         if keys_detected[i] != keys_detected[i + 1])
        
        # Key distribution
        key_counts = Counter(keys_detected)
        total = len(keys_detected)
        key_distribution = {k: v / total for k, v in key_counts.items()}
        
        # Primary key (most common)
        primary_key = key_counts.most_common(1)[0][0]
        stability_score = key_counts[primary_key] / total
        
        return {
            "primary_key": primary_key,
            "key_changes": key_changes,
            "stability_score": stability_score,
            "key_distribution": key_distribution,
            "keys_timeline": keys_detected
        }


class HarmonicAnalyzer:
    """Analyze harmonic content and coherence"""
    
    # Interval consonance ratings (0=dissonant, 1=consonant)
    CONSONANCE_MAP = {
        0: 1.0,   # Unison
        1: 0.1,   # Minor 2nd
        2: 0.3,   # Major 2nd
        3: 0.5,   # Minor 3rd
        4: 0.7,   # Major 3rd
        5: 0.6,   # Perfect 4th
        6: 0.2,   # Tritone
        7: 0.8,   # Perfect 5th
        8: 0.6,   # Minor 6th
        9: 0.7,   # Major 6th
        10: 0.4,  # Minor 7th
        11: 0.3,  # Major 7th
    }
    
    @classmethod
    def analyze_harmony(cls, notes: List[Tuple[int, int, int]], 
                       time_window_ms: int = 100) -> Dict:
        """
        Analyze harmonic coherence by examining simultaneous notes
        """
        if not notes:
            return {
                "harmonic_coherence": 0.0,
                "avg_consonance": 0.0,
                "dissonance_count": 0,
                "chord_diversity": 0.0
            }
        
        # Group notes by time windows
        time_buckets = defaultdict(list)
        for time, pitch, velocity in notes:
            bucket = time // time_window_ms
            time_buckets[bucket].append(pitch)
        
        # Analyze each time slice
        consonance_scores = []
        chord_types = []
        
        for bucket, pitches in time_buckets.items():
            if len(pitches) < 2:
                continue
            
            # Calculate intervals between all pairs
            intervals = []
            for i in range(len(pitches)):
                for j in range(i + 1, len(pitches)):
                    interval = abs(pitches[i] - pitches[j]) % 12
                    intervals.append(interval)
            
            # Average consonance for this slice
            if intervals:
                slice_consonance = np.mean([cls.CONSONANCE_MAP.get(iv, 0.5) 
                                           for iv in intervals])
                consonance_scores.append(slice_consonance)
            
            # Identify chord type (simplified)
            pitch_classes = sorted(set(p % 12 for p in pitches))
            chord_types.append(tuple(pitch_classes))
        
        # Calculate metrics
        avg_consonance = np.mean(consonance_scores) if consonance_scores else 0.5
        dissonance_count = sum(1 for c in consonance_scores if c < 0.4)
        
        # Chord diversity (unique chord types / total chords)
        chord_diversity = len(set(chord_types)) / max(len(chord_types), 1)
        
        # Overall harmonic coherence (weighted combination)
        harmonic_coherence = (
            avg_consonance * 0.6 +
            (1 - dissonance_count / max(len(consonance_scores), 1)) * 0.3 +
            min(chord_diversity, 0.5) * 0.1 * 2  # Penalize too much diversity
        ) * 100
        
        return {
            "harmonic_coherence": harmonic_coherence,
            "avg_consonance": avg_consonance,
            "dissonance_count": dissonance_count,
            "chord_diversity": chord_diversity,
            "total_chords": len(chord_types)
        }


class RhythmicAnalyzer:
    """Analyze rhythmic patterns and consistency"""
    
    @classmethod
    def analyze_rhythm(cls, notes: List[Tuple[int, int, int]], 
                      ticks_per_beat: int = 480) -> Dict:
        """
        Analyze rhythmic consistency and patterns
        """
        if len(notes) < 2:
            return {
                "rhythmic_consistency": 0.0,
                "tempo_bpm": 120.0,
                "tempo_variance": 0.0,
                "syncopation_score": 0.0,
                "note_density": 0.0
            }
        
        # Extract inter-onset intervals (IOI)
        iois = []
        for i in range(len(notes) - 1):
            ioi = notes[i + 1][0] - notes[i][0]  # Time difference in ticks
            if ioi > 0:
                iois.append(ioi)
        
        if not iois:
            return {
                "rhythmic_consistency": 0.0,
                "tempo_bpm": 120.0,
                "tempo_variance": 0.0,
                "syncopation_score": 0.0,
                "note_density": 0.0
            }
        
        # Rhythmic consistency (inverse of IOI coefficient of variation)
        mean_ioi = np.mean(iois)
        std_ioi = np.std(iois)
        cv = std_ioi / mean_ioi if mean_ioi > 0 else 0
        rhythmic_consistency = max(0, 100 - cv * 100)
        
        # Estimate tempo from IOIs
        # Assume mean IOI corresponds to some beat subdivision
        beat_ioi = mean_ioi  # Simplified: treat as quarter notes
        tempo_bpm = 60 / (beat_ioi / ticks_per_beat) if beat_ioi > 0 else 120
        
        # Tempo variance
        tempo_variance = std_ioi / ticks_per_beat if ticks_per_beat > 0 else 0
        
        # Syncopation score: measure off-beat emphasis
        on_beats = sum(1 for time, _, _ in notes if (time % ticks_per_beat) < ticks_per_beat * 0.1)
        off_beats = len(notes) - on_beats
        syncopation_score = off_beats / len(notes) if notes else 0
        
        # Note density (notes per second)
        duration_ticks = notes[-1][0] - notes[0][0]
        duration_seconds = duration_ticks / ticks_per_beat / (tempo_bpm / 60)
        note_density = len(notes) / max(duration_seconds, 0.1)
        
        return {
            "rhythmic_consistency": rhythmic_consistency,
            "tempo_bpm": tempo_bpm,
            "tempo_variance": tempo_variance,
            "syncopation_score": syncopation_score,
            "note_density": note_density,
            "mean_ioi_ticks": mean_ioi,
            "ioi_cv": cv
        }


class StructuralAnalyzer:
    """Analyze overall musical structure"""
    
    @classmethod
    def analyze_structure(cls, events: List) -> Dict:
        """
        Analyze structural properties of MIDI sequence
        """
        notes = []
        control_changes = []
        program_changes = []
        time_signatures = []
        tempos = []
        
        current_time = 0
        
        for event in events:
            if not event:
                continue
            
            event_type = event[0] if len(event) > 0 else None
            delta = event[1] if len(event) > 1 else 0
            current_time += delta
            
            if event_type == "note":
                if len(event) >= 6:
                    pitch = event[4]
                    velocity = event[5]
                    notes.append((current_time, pitch, velocity))
            
            elif event_type == "control_change":
                control_changes.append(current_time)
            
            elif event_type == "patch_change":
                if len(event) >= 6:
                    program = event[5]
                    program_changes.append((current_time, program))
            
            elif event_type == "time_signature":
                time_signatures.append(current_time)
            
            elif event_type == "set_tempo":
                if len(event) >= 5:
                    bpm = event[4]
                    tempos.append((current_time, bpm))
        
        # Calculate metrics
        duration_ticks = current_time
        duration_seconds = duration_ticks / 480.0  # Simplified
        
        # Event type distribution
        total_events = len(events)
        event_distribution = {
            "notes": len(notes),
            "control_changes": len(control_changes),
            "program_changes": len(program_changes),
            "time_signatures": len(time_signatures),
            "tempo_changes": len(tempos)
        }
        
        # Density metrics
        note_density = len(notes) / max(duration_seconds, 0.1)
        cc_density = len(control_changes) / max(duration_seconds, 0.1)
        
        # Pitch statistics
        if notes:
            pitches = [n[1] for n in notes]
            pitch_range = max(pitches) - min(pitches)
            pitch_mean = np.mean(pitches)
            pitch_std = np.std(pitches)
        else:
            pitch_range = 0
            pitch_mean = 60
            pitch_std = 0
        
        # Velocity statistics
        if notes:
            velocities = [n[2] for n in notes]
            velocity_mean = np.mean(velocities)
            velocity_std = np.std(velocities)
        else:
            velocity_mean = 64
            velocity_std = 0
        
        return {
            "total_events": total_events,
            "duration_seconds": duration_seconds,
            "event_distribution": event_distribution,
            "note_density": note_density,
            "cc_density": cc_density,
            "pitch_range": pitch_range,
            "pitch_mean": pitch_mean,
            "pitch_std": pitch_std,
            "velocity_mean": velocity_mean,
            "velocity_std": velocity_std,
            "program_changes": program_changes,
            "tempo_changes": tempos
        }


def comprehensive_analysis(events: List) -> Dict:
    """
    Perform all musical analyses and return comprehensive metrics
    """
    # Extract notes for detailed analysis
    notes = []
    current_time = 0
    
    for event in events:
        if not event:
            continue
        
        event_type = event[0] if len(event) > 0 else None
        delta = event[1] if len(event) > 1 else 0
        current_time += delta
        
        if event_type == "note" and len(event) >= 6:
            pitch = event[4]
            velocity = event[5]
            notes.append((current_time, pitch, velocity))
    
    # Run all analyses
    key_analysis = KeyDetector.analyze_key_stability(notes)
    harmonic_analysis = HarmonicAnalyzer.analyze_harmony(notes)
    rhythmic_analysis = RhythmicAnalyzer.analyze_rhythm(notes)
    structural_analysis = StructuralAnalyzer.analyze_structure(events)
    
    # Combine results
    return {
        "key_analysis": key_analysis,
        "harmonic_analysis": harmonic_analysis,
        "rhythmic_analysis": rhythmic_analysis,
        "structural_analysis": structural_analysis,
        "note_count": len(notes)
    }


if __name__ == "__main__":
    # Test with sample data
    print("Musical Analysis Module - Test Mode")
    
    # Create sample notes (C major scale)
    test_notes = [
        (0, 60, 100),    # C
        (480, 62, 100),  # D
        (960, 64, 100),  # E
        (1440, 65, 100), # F
        (1920, 67, 100), # G
        (2400, 69, 100), # A
        (2880, 71, 100), # B
        (3360, 72, 100), # C
    ]
    
    key, confidence = KeyDetector.detect_key(test_notes)
    print(f"Detected Key: {key} (confidence: {confidence:.2f})")
    
    harmony = HarmonicAnalyzer.analyze_harmony(test_notes)
    print(f"Harmonic Coherence: {harmony['harmonic_coherence']:.1f}")
    
    rhythm = RhythmicAnalyzer.analyze_rhythm(test_notes)
    print(f"Rhythmic Consistency: {rhythm['rhythmic_consistency']:.1f}")
    print(f"Estimated Tempo: {rhythm['tempo_bpm']:.1f} BPM")
