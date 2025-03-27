from fastapi import FastAPI
from pydantic import BaseModel
import spacy

# Carregar o modelo de linguagem
nlp = spacy.load("en_core_web_sm")

app = FastAPI()

class TextRequest(BaseModel):
    text: str

@app.post("/get_sentences/")
async def process_text(request: TextRequest):
    text = request.text
    doc = nlp(text)
    sentences = [sent.text for sent in doc.sents]
    # Aqui você pode adicionar a lógica de semantic chunking
    return {"sentences": sentences}

