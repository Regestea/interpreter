
declare global {
    interface Blob {
        getDurationSeconds(): Promise<number>;
    }
}

Blob.prototype.getDurationSeconds = async function (): Promise<number> {
    try {
        const arrayBuffer = await this.arrayBuffer();
        const audioContext = new AudioContext();

        try {
            const audioBuffer = await audioContext.decodeAudioData(arrayBuffer);
            const duration = audioBuffer.duration;
            
            return Math.round(duration * 10) / 10;
        } finally {
            await audioContext.close();
        }
    } catch (error) {
        console.error("Error getting audio duration:", error);
        return 0;
    }
};

export {};