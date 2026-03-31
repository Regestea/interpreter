import React, { useState, useCallback } from 'react';
import { useSpeechSegmenter } from "../hooks/useSpeechSegmenter.ts";

const SpeechDictation: React.FC = () => {
    const [audioSegments, setAudioSegments] = useState<Blob[]>([]);

    const handleSegmentReady = useCallback((segment: Blob) => {
        setAudioSegments(prev => [...prev, segment]);
    }, []);

    const {
        isListening,
        isRecording,
        isCalibrating,
        startListening,
        stopListening,
        error,
    } = useSpeechSegmenter({
        chunkDurationMs: 200,
        silenceThresholdChunks: 8,
        onSegmentReady: handleSegmentReady,
    });

    const clearSegments = useCallback(() => {
        setAudioSegments([]);
    }, []);

    return (
        <div style={{ padding: '20px', fontFamily: 'sans-serif', maxWidth: '600px', margin: '0 auto' }}>
            <h2>Speech Segmentation Dictation</h2>

            <div style={{ display: 'flex', gap: '10px', marginBottom: '20px' }}>
                <button
                    onClick={startListening}
                    disabled={isListening}
                    style={{
                        padding: '10px 20px',
                        cursor: isListening ? 'not-allowed' : 'pointer',
                        backgroundColor: isListening ? '#ccc' : '#4CAF50',
                        color: 'white',
                        border: 'none',
                        borderRadius: '4px'
                    }}
                >
                    Start Listening
                </button>

                <button
                    onClick={stopListening}
                    disabled={!isListening}
                    style={{
                        padding: '10px 20px',
                        cursor: !isListening ? 'not-allowed' : 'pointer',
                        backgroundColor: !isListening ? '#ccc' : '#f44336',
                        color: 'white',
                        border: 'none',
                        borderRadius: '4px'
                    }}
                >
                    Stop Listening
                </button>

                {audioSegments.length > 0 && (
                    <button
                        onClick={clearSegments}
                        style={{
                            padding: '10px 20px',
                            cursor: 'pointer',
                            backgroundColor: '#ff9800',
                            color: 'white',
                            border: 'none',
                            borderRadius: '4px'
                        }}
                    >
                        Clear All
                    </button>
                )}
            </div>

            <div style={{ marginBottom: '20px', padding: '10px', backgroundColor: '#f0f0f0', borderRadius: '5px' }}>
                <strong>System Status: </strong>
                {!isListening && <span>🔴 Offline (Press Start)</span>}
                {isListening && isCalibrating && <span style={{ color: 'orange' }}>⚙️ Calibrating Environment Noise... Please be quiet.</span>}
                {isListening && !isCalibrating && !isRecording && <span style={{ color: 'blue' }}>🟢 Listening... (Waiting for speech)</span>}
                {isListening && !isCalibrating && isRecording && <span style={{ color: 'red' }}>🎙️ Recording Speech...</span>}
            </div>

            {error && (
                <div style={{ color: 'red', marginBottom: '20px', padding: '10px', backgroundColor: '#ffebee', borderRadius: '5px' }}>
                    <strong>Error:</strong> {error}
                </div>
            )}

            <div>
                <h3>Recorded Segments ({audioSegments.length})</h3>
                {audioSegments.length === 0 ? (
                    <p style={{ color: 'gray' }}>No audio segments recorded yet.</p>
                ) : (
                    <ul style={{ listStyleType: 'none', padding: 0 }}>
                        {audioSegments.map((blob, index) => (
                            <li key={index} style={{ marginBottom: '15px', padding: '10px', border: '1px solid #ccc', borderRadius: '5px' }}>
                                <div style={{ marginBottom: '5px', fontWeight: 'bold' }}>
                                    Segment #{index + 1} ({(blob.size / 1024).toFixed(2)} KB)
                                </div>
                                <audio src={URL.createObjectURL(blob)} controls style={{ width: '100%' }} />
                            </li>
                        ))}
                    </ul>
                )}
            </div>
        </div>
    );
};

export default SpeechDictation;
