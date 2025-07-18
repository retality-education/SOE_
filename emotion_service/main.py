﻿from fastapi import FastAPI
from pydantic import BaseModel
from transformers import pipeline
from googletrans import Translator
import uvicorn

app = FastAPI()

# Инициализация переводчика и классификатора
translator = Translator()
classifier = pipeline("text-classification", model="boltuix/bert-emotion")

class TextRequest(BaseModel):
    text: str

async def translate_ru_to_en(text: str) -> str:
    try:
        translated = await translator.translate(text, src='ru', dest='en')
        return translated.text
    except Exception as e:
        print("Ошибка при переводе:", e)
        raise

@app.get("/health")
async def health():
    return {"status": "ok"}

@app.post("/analyze")
async def analyze(request: TextRequest):
    translated = await translate_ru_to_en(request.text)
    result = classifier(translated)[0]
    return {
        "original_text": request.text,
        "translated_text": translated,
        "emotion": result["label"],
        "confidence": result["score"]
    }

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=5001)
