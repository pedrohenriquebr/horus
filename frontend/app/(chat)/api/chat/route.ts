
import { auth } from '@/app/(auth)/auth';
import { systemPrompt } from '@/lib/ai/prompts';
import {
  deleteChatById,
  getChatById,
  saveChat,
  saveMessages,
} from '@/lib/db/queries';
import {
  generateUUID,
  getMostRecentUserMessage,
  sanitizeResponseMessages,
} from '@/lib/utils';
import { generateTitleFromUserMessage } from '../../actions';
import { createDocument } from '@/lib/ai/tools/create-document';
import { updateDocument } from '@/lib/ai/tools/update-document';
import { requestSuggestions } from '@/lib/ai/tools/request-suggestions';
import { getWeather } from '@/lib/ai/tools/get-weather';
import { isProductionEnvironment } from '@/lib/constants';
import { NextResponse } from 'next/server';
import { myProvider } from '@/lib/ai/providers';
import { HorusApiService } from '@/lib/horus/api-service';
import { GenerateTextQuery } from '@/lib/horus/interfaces';
import { Message } from '@/lib/db/schema';

export const maxDuration = 60;

export async function POST(request: Request) {
  try {
    const {
      id: chatId,
      messages,
      selectedChatModel,
    }: {
      id: string;
      messages: Array<Message>;
      selectedChatModel: string;
    } = await request.json();

    const session = await auth();

    if (!session || !session.user || !session.user.id) {
      return new Response('Unauthorized', { status: 401 });
    }

    const userMessage = getMostRecentUserMessage(messages);

    if (!userMessage) {
      return new Response('No user message found', { status: 400 });
    }

    const chat = await getChatById({ id: chatId });
    if (!chat) {
      const title = await generateTitleFromUserMessage({ message: userMessage  });
      await saveChat({ id: chatId, userId: session.user.id, title });
    } 
    // else if (chat.userId !== session.user.id) {
    //   return new Response('Unauthorized', { status: 401 });
    // }

    // await saveMessages({
    //   messages: [{ ...userMessage, createdAt: new Date(), chatId: chatId }],
    // });

    const generateTextQuery : GenerateTextQuery = {
      prompt: userMessage.content as string,
      userInfo: { 
        id: session.user.id,
        chatSessionId: chatId
      },
      // systemInstructions: systemPrompt({ selectedChatModel }),
    };
    
    const horus = new HorusApiService(process.env.HORUS_BASEURL!);

    const response = await horus.generateText(generateTextQuery);
    const generatedText = response.text;
    const sources = response.sources;

    // await saveMessages({
    //   messages: [{
    //     id: generateUUID(),
    //     chatId: chatId,
    //     role: 'assistant',
    //     content: generatedText,
    //     createdAt: new Date(),
    //   }],
    // });
    
    return NextResponse.json({ text: generatedText, sources: response.sources });

  } catch (error) {
    console.error(error);
    return NextResponse.json({ error: 'Failed to process request' }, { status: 400 });
  }
}
    
export async function DELETE(request: Request) {
  const { searchParams } = new URL(request.url);
  const id = searchParams.get('id');
  console.info('id recebido na rota de delete:', id)
  if (!id) {
    return new Response('Not Found', { status: 404 });
  }

  const session = await auth();

  if (!session || !session.user) {
    return new Response('Unauthorized', { status: 401 });
  }

  try {
    const chat = await getChatById({ id });

    //TODO: check if the user is the owner of the chat
    // if (chat.userId !== session.user.id) {
    //   return new Response('Unauthorized', { status: 401 });
    // }

    await deleteChatById({ id });

    return new Response('Chat deleted', { status: 200 });
  } catch (error) {
    return new Response('An error occurred while processing your request', {
      status: 500,
    });
  }
}
