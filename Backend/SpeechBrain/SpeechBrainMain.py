import io
import torch
import librosa
import soundfile as sf
from speechbrain.inference import SpeakerRecognition
from torch.nn.functional import cosine_similarity

SR = 16000
THRESHOLD = 0.5
model = None

def init():
    global model
    model = SpeakerRecognition.from_hparams(
        source="speechbrain/spkrec-ecapa-voxceleb",
        savedir="tmpdir",
        run_opts={"device": "cuda" if torch.cuda.is_available() else "cpu"},
    )
    _ = model.modules

def load_audio_bytes(data: bytes, sr=SR):
    # فایل را از bytes باز می‌کنیم
    with io.BytesIO(data) as f:
        sig, file_sr = sf.read(f)
        # اگر استریو بود، تبدیل به مونو
        if len(sig.shape) > 1:
            sig = sig.mean(axis=1)
        # ریسمپل
        sig = librosa.resample(sig, orig_sr=file_sr, target_sr=sr)
        return sig

def get_embedding(sig):
    tensor = torch.tensor(sig, dtype=torch.float32).unsqueeze(0)
    emb = model.encode_batch(tensor)
    v = emb.detach().cpu().squeeze()
    if v.dim() != 1:
        v = v.flatten()
    return v.float()

def get_audio_embedding(audio_bytes: bytes):
    """
    Gets the audio embedding from audio bytes.
    Returns the embedding as a list that can be passed to compare_bytes.
    """
    sig = load_audio_bytes(audio_bytes)
    embedding = get_embedding(sig)
    return embedding.tolist()

def compare_bytes(audio_bytes: bytes, main_embedding_list: list):
    """
    Compares the given audio bytes with the provided main audio embedding.
    
    Args:
        audio_bytes: Audio file as bytes to compare
        main_embedding_list: The main audio embedding as a list (from get_audio_embedding)
    
    Returns:
        Dictionary with comparison results
    """
    if main_embedding_list is None or len(main_embedding_list) == 0:
        return {
            "score": 0.0,
            "is_match": False,
            "status": "error",
            "message": "Main audio embedding not provided or empty."
        }
    
    # Convert main_embedding_list back to tensor
    main_audio_embedding = torch.tensor(main_embedding_list, dtype=torch.float32)
    
    sig = load_audio_bytes(audio_bytes)
    emb = get_embedding(sig)

    # align lengths
    m = min(main_audio_embedding.numel(), emb.numel())
    emb1 = main_audio_embedding[:m]
    emb2 = emb[:m]

    score = float(cosine_similarity(emb1, emb2, dim=0).item())
    return {
        "score": score,
        "is_match": score > THRESHOLD,
        "status": "success"
    }
