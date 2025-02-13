-- Adiciona colunas para memórias e histórico de chat
ALTER TABLE request_response_log
    ADD COLUMN used_memories TEXT;
ALTER TABLE request_response_log
    ADD COLUMN working_memories TEXT;
ALTER TABLE request_response_log
    ADD COLUMN chat_history TEXT;
