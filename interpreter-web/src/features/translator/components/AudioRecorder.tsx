import React from 'react';
import {useSpeechSegmenter} from "../hooks/useAudioBuffer.ts";

const AudioRecorderComponent: React.FC = () => {
    const {
        isListening,
        isRecording,
        audioSegments,
        startListening,
        stopListening,
        error,
    } = useSpeechSegmenter({
        silenceDurationMs: 3000,
        volumeThreshold: 15,
    });

    return (
        <div style={{ padding: '20px', fontFamily: 'sans-serif', direction: 'rtl' }}>
            <h2>ضبط هوشمند دیالوگ‌ها</h2>

            {error && <p style={{ color: 'red' }}>{error}</p>}

            <div style={{ marginBottom: '20px' }}>
                <button
                    onClick={isListening ? stopListening : startListening}
                    style={{
                        padding: '10px 20px',
                        backgroundColor: isListening ? '#f44336' : '#4CAF50',
                        color: 'white',
                        border: 'none',
                        borderRadius: '5px',
                        cursor: 'pointer',
                        fontSize: '16px'
                    }}
                >
                    {isListening ? 'توقف شنود میکروفون' : 'شروع شنود میکروفون'}
                </button>
            </div>

            <div style={{ marginBottom: '30px' }}>
                <strong>وضعیت سیستم: </strong>
                {isListening ? (
                    <span style={{ color: isRecording ? '#4CAF50' : '#FF9800', fontWeight: 'bold' }}>
            {isRecording ? 'در حال ضبط صحبت...' : 'در انتظار صحبت (سکوت)...'}
          </span>
                ) : (
                    <span style={{ color: 'gray' }}>خاموش</span>
                )}
            </div>

            <div>
                <h3>دیالوگ‌های ضبط شده ({audioSegments.length})</h3>

                {/* کانتینر ردیفی برای نمایش تکه صداها */}
                <div
                    style={{
                        display: 'flex',
                        flexWrap: 'wrap',
                        gap: '20px',
                        marginTop: '15px'
                    }}
                >
                    {audioSegments.map((blob, index) => {
                        const url = URL.createObjectURL(blob);
                        return (
                            <div
                                key={index}
                                style={{
                                    display: 'flex',
                                    flexDirection: 'column',
                                    alignItems: 'center',
                                    padding: '15px',
                                    border: '1px solid #ddd',
                                    borderRadius: '8px',
                                    backgroundColor: '#f9f9f9',
                                    boxShadow: '0 2px 4px rgba(0,0,0,0.05)'
                                }}
                            >
                <span style={{ marginBottom: '10px', fontWeight: 'bold' }}>
                  قطعه $ {index + 1} $
                </span>
                                {/* پلیر پخش صدا */}
                                <audio controls src={url} style={{ width: '250px' }} />
                            </div>
                        );
                    })}
                </div>

                {audioSegments.length === 0 && (
                    <p style={{ color: '#888' }}>هنوز هیچ دیالوگی ضبط نشده است.</p>
                )}
            </div>
        </div>
    );
};

export default AudioRecorderComponent;
