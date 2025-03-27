# Escolher a imagem base com Python
FROM python:3.10-slim

# Definir o diretório de trabalho dentro do container
WORKDIR /app

# Copiar os requisitos do projeto para dentro do container
COPY python_services/requirements.txt /app/requirements.txt

# Instalar as dependências do projeto
RUN pip install --no-cache-dir -r /app/requirements.txt

# Baixar o modelo do spaCy
RUN python -m spacy download en_core_web_sm

# Copiar o código da aplicação para dentro do container
COPY python_services /app

# Expor a porta 8000 (a porta que o FastAPI irá rodar)
EXPOSE 8000

# Comando para rodar o FastAPI no Uvicorn
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
