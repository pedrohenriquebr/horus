'use client';

import type { Attachment } from 'ai';
import { useChat } from '@ai-sdk/react';
import { useState } from 'react';
import useSWR, { useSWRConfig } from 'swr';
import { ChatHeader } from '@/components/chat-header';
import type { Message, Vote } from '@/lib/db/schema';
import { fetcher, generateUUID } from '@/lib/utils';
import { Artifact } from './artifact';
import { MultimodalInput } from './multimodal-input';
import { Messages } from './messages';
import { VisibilityType } from './visibility-selector';
import { useArtifactSelector } from '@/hooks/use-artifact';
import { toast } from 'sonner';

export function Chat({
  id,
  initialMessages,
  selectedChatModel,
  selectedVisibilityType,
  isReadonly,
}: {
  id: string;
  initialMessages: Array<Message>;
  selectedChatModel: string;
  selectedVisibilityType: VisibilityType;
  isReadonly: boolean;
}) {
  const { mutate } = useSWRConfig();

  // Manual state management
  const [messages, setMessages] = useState(initialMessages);
  const [input, setInput] = useState('');
  const [status, setStatus] = useState<'ready' | 'streaming' | 'error' | 'submitted'>('ready');
  const [attachments, setAttachments] = useState<Array<Attachment>>([]);

  const { data: votes } = useSWR<Array<Vote>>(`/api/vote?chatId=${id}`, fetcher);
  const isArtifactVisible = useArtifactSelector((state) => state.isVisible);

  // Handle form submission
  const handleSubmit = async (e: React.FormEvent) => {
    // e.preventDefault();
    if (!input.trim()) return;

    const newMessage = {
      id: generateUUID(),
      role: 'user',
      content: input,
      createdAt: new Date(),
    } as Message;

    setMessages((prev) => [...prev, newMessage]);
    setInput('');
    setStatus('submitted');

    try {
      const response = await fetch('/api/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          id,
          messages: [...messages, newMessage],
          selectedChatModel,
        }),
      });

     

      const data = await response.json();
      const generatedText = data.text;
      const sources = data.sources;

      const assistantMessage = {
        id: generateUUID(),
        role: 'assistant',
        content: generatedText,
        createdAt: new Date(),
        sources
      } as Message;

      setMessages((prev) => [...prev, assistantMessage]);
      setStatus('ready');
      mutate('/api/history');
    } catch (error) {
      console.error(error);
      toast.error('An error occurred, please try again!');
      setStatus('error');
    }
  };

  return (
    <>
      <div className="flex flex-col min-w-0 h-dvh bg-background">
        <ChatHeader
          chatId={id}
          selectedModelId={selectedChatModel}
          selectedVisibilityType={selectedVisibilityType}
          isReadonly={isReadonly}
        />

        <Messages
          chatId={id}
          status={status}
          votes={votes}
          messages={messages}
          setMessages={setMessages}
          isReadonly={isReadonly}
          isArtifactVisible={isArtifactVisible}
        />

        <form
          className="flex mx-auto px-4 bg-background pb-4 md:pb-6 gap-2 w-full md:max-w-3xl"
          onSubmit={handleSubmit}
        >
          {!isReadonly && (
            <MultimodalInput
              chatId={id}
              input={input}
              setInput={setInput}
              handleSubmit={handleSubmit}
              status={status}
              attachments={attachments}
              setAttachments={setAttachments}
              messages={messages}
              setMessages={setMessages}
            />
          )}
        </form>
      </div>

      <Artifact
        chatId={id}
        input={input}
        setInput={setInput}
        handleSubmit={handleSubmit}
        status={status}
        attachments={attachments}
        setAttachments={setAttachments}
        messages={messages}
        setMessages={setMessages}
        votes={[]}
        isReadonly={isReadonly}
      />
    </>
  );
}