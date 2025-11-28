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

def compare_bytes(b1: bytes, b2: bytes):
    sig1 = load_audio_bytes(b1)
    sig2 = load_audio_bytes(b2)

    emb1 = get_embedding(sig1)
    emb2 = get_embedding(sig2)

    # align lengths
    m = min(emb1.numel(), emb2.numel())
    emb1 = emb1[:m]
    emb2 = emb2[:m]

    score = float(cosine_similarity(emb1, emb2, dim=0).item())
    return {
        "score": score,
        "is_match": score > THRESHOLD,
        "status": "success"
    }
