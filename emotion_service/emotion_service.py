# -*- coding: utf-8 -*-
import os
import logging
from fastapi import FastAPI
from pydantic import BaseModel
import uvicorn
from transformers import pipeline

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = Fastapi()

# Указываем абсолютный путь для кеша
os.environ['HF_HOME'] = 'C:/hf_cache'
os.makedirs(os.environ['HF_HOME'], exist_ok=True)

try:
    logger.info("Загрузка модели перевода...")
    translator = pipeline(
        "translation_ru_to_en",
        model="Helsinki-NLP/opus-mt-ru-en",
        device="cpu"
    )
    
    logger.info("Загрузка анализатора эмоций...")
    emotion_analyzer = pipeline(
        "text-classification", 
        model="distilbert-base-uncased-finetuned-sst-2-english",
        device="cpu"
    )
    
    logger.info("Модели успешно загружены!")
except Exception as e:
    logger.error(f"ФАТАЛЬНАЯ ОШИБКА: {str(e)}")
    raise RuntimeError("Не удалось загрузить модели. Проверьте подключение к интернету")

class TextRequest(BaseModel):
    text: str

@app.post("/analyze")
async def analyze(request: TextRequest):
    try:
        # Простейший анализ без перевода (для теста)
        if "рад" in request.text.lower():
            return {"emotion": "joy", "confidence": 0.99}
        elif "груст" in request.text.lower():
            return {"emotion": "sadness", "confidence": 0.95}
        else:
            return {"emotion": "neutral", "confidence": 0.8}
            
    except Exception as e:
        return {"error": str(e)}

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=5001)