FROM postgres:13

# Instalar as dependências necessárias
RUN apt-get update \
    && apt-get install -y \
    build-essential \
    postgresql-server-dev-13 \
    git

# Clonar o repositório pgvector e compilar
RUN git clone --branch v0.8.0 https://github.com/pgvector/pgvector.git /pgvector \
    && cd /pgvector \
    && make \
    && make install \
    && rm -rf /pgvector


# Copiar o arquivo de inicialização para o PostgreSQL
COPY ./init.sql /docker-entrypoint-initdb.d/


# Configurar permissões para arquivos de inicialização
RUN chmod -R 755 /docker-entrypoint-initdb.d


EXPOSE 5432