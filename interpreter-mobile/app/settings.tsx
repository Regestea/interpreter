import React, { useEffect, useState } from 'react';
import { View, Text, StyleSheet, Platform, FlatList, Button } from 'react-native';
import * as Audio from 'expo-audio'; // Correct import for expo-audio

// Define types for microphone objects
interface Microphone {
    id: string;
    name: string;
}

export default function MicrophoneList():Element {
    const [mics, setMics] = useState<Microphone[]>([]);
    const [permissionGranted, setPermissionGranted] = useState<boolean>(false);

    const getMicrophones = async (): Promise<void> => {
        if (Platform.OS === 'web') {
            // --- WEB SOLUTION ---
            try {
                // Request permission first
                const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                // In web, we can stop the stream if we only need to check permissions and enumerate devices
                stream.getTracks().forEach(track => track.stop());

                // Enumerate devices
                const devices = await navigator.mediaDevices.enumerateDevices();
                const audioInputs = devices.filter(device => device.kind === 'audioinput');

                setMics(audioInputs.map(d => ({ id: d.deviceId, name: d.label || `Microphone ${d.deviceId}` })));
            } catch (err: any) {
                console.error("Web Mic Error:", err);
                alert(`Error accessing microphones: ${err.message}`);
            }
        } else {
            // --- ANDROID / iOS SOLUTION ---
            try {
                const permission = await Audio.requestRecordingPermissionsAsync(); // Request permission using expo-audio
                if (permission.granted) {
                    setPermissionGranted(true);
                    // Simulating a list since we can't get the real hardware list easily in standard Expo
                    setMics([
                        { id: 'default', name: 'Default System Microphone (OS Selected)' }
                    ]);
                } else {
                    setPermissionGranted(false);
                    alert("Permission denied");
                }
            } catch (err) {
                console.error("Permission Error:", err);
                alert(`Error requesting microphone permission: ${err.message}`);
            }
        }
    };

    useEffect(() => {
        getMicrophones();
    }, []);

    return (
        <View style={styles.container}>
            <Text style={styles.title}>Microphone List</Text>
            <Button title="Refresh / Load Mics" onPress={getMicrophones} />

            <FlatList
                data={mics}
                keyExtractor={(item) => item.id}
                renderItem={({ item }) => (
                    <View style={styles.item}>
                        <Text style={styles.text}>{item.name}</Text>
                    </View>
                )}
            />
            {!permissionGranted && Platform.OS !== 'web' && (
                <Text style={styles.warningText}>Microphone permission is required to list microphones.</Text>
            )}
        </View>
    );
}

const styles = StyleSheet.create({
    container: { flex: 1, padding: 20, justifyContent: 'center' },
    title: { fontSize: 20, marginBottom: 20, textAlign: 'center' },
    item: { padding: 15, borderBottomWidth: 1, borderBottomColor: '#ccc' },
    text: { fontSize: 16 },
    warningText: { color: 'red', textAlign: 'center', marginTop: 10 }
});
