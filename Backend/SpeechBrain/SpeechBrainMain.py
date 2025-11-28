import io
import torch
import librosa
import soundfile as sf
from speechbrain.inference import SpeakerRecognition
from torch.nn.functional import cosine_similarity

SR = 16000
THRESHOLD = 0.5
model = None
main_audio_embedding = None

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

def set_main_model_embedding(audio_bytes: bytes):
    """
    Sets the main audio embedding from audio bytes.
    This should be called before using compare_bytes.
    """
    global main_audio_embedding
    sig = load_audio_bytes(audio_bytes)
    main_audio_embedding = get_embedding(sig)
    return {
        "status": "success",
        "message": "Main audio embedding set successfully"
    }

def compare_bytes(audio_bytes: bytes):
    """
    Compares the given audio bytes with the main audio embedding.
    Raises an error if main_audio_embedding is not set.
    """
    global main_audio_embedding
    
    if main_audio_embedding is None:
        return {
            "score": 0.0,
            "is_match": False,
            "status": "error",
            "message": "Main audio embedding not set. Call set_main_model_embedding first."
        }
    
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
