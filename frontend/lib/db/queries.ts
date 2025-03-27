import 'server-only';

import { genSaltSync, hashSync } from 'bcrypt-ts';
import { and, asc, desc, eq, gt, gte, inArray } from 'drizzle-orm';
import { drizzle } from 'drizzle-orm/postgres-js';
import postgres from 'postgres';

import {
  type User,
  type Suggestion,
  type Message,
  Chat,
} from './schema';
import { ArtifactKind } from '@/components/artifact';
import { HorusApiService } from '../horus/api-service';
import { ValidateCredentialsResponse } from '../horus/interfaces';


// Optionally, if not using email/pass login, you can
// use the Drizzle adapter for Auth.js / NextAuth
// https://authjs.dev/reference/adapter/drizzle

// biome-ignore lint: Forbidden non-null assertion.
const horus = new HorusApiService(process.env.HORUS_BASEURL!);

export async function getUser(email: string): Promise<Array<User>> {
  try {
    var response = await horus.getUserByEmail(email);

    if(response == null) {
      return [];
    }

    return [response].map(d => {
      return {
        id: d.id,
        email: d.email,
        password: d.passwordHash,
      } 
    })

  } catch (error) {
    console.error('Failed to get user from database');
    console.error(error);
    throw error;
  }
}


  export async function validateUser(email: string, password: string): Promise<ValidateCredentialsResponse> {
    try {
      var response = await horus.validateUser(email, password);

      return response
    } catch (error) {
      console.error('Failed to get user from database');
      console.error(error);
      throw error;
    }
  }

export async function createUser(email: string, password: string, name: string) {
  try {
    return await horus.registerUser({email, password, name});
  } catch (error) {
    console.error('Failed to create user in database');
    throw error;
  }
}

export async function saveChat({
  id,
  userId,
  title,
}: {
  id: string;
  userId: string;
  title: string;
}) {
  try {
    //TODO: pass user id
    return await horus.createChatSession({
      id,
      projectId: null,
      title: title,
      endedAt: null,
      metadata: {
        userId
      }
    })
  } catch (error) {
    console.error('Failed to save chat in database');
    throw error;
  }
}

export async function deleteChatById({ id }: { id: string }) {
  try {
    await horus.deleteChatSession(id);
  } catch (error) {
    console.error('Failed to delete chat by id from database');
    throw error;
  }
}

export async function getChatsByUserId({ id }: { id: string }) {
  try {
    var response=  await horus.getChatSessiosByUserId(id, {offset: 1, pageSize: 100});

    return response.items
  } catch (error) {
    console.error('Failed to get chats by user from database');
    throw error;
  }
}

export async function getChatById({ id }: { id: string }) : Promise<Chat> {
  try {
    // const [selectedChat] = await db.select().from(chat).where(eq(chat.id, id));
    console.info('trying to delete', id);
    var response = await horus.getChatSession(id);
    if (response == null) {
      return null;
    }

    return {
      createdAt: new Date(response.createdAt),
      id: response.id,
      title: response.title,
      userId: '',
      visibility: 'public',
    }
  } catch (error) {
    console.error('Failed to get chat by id from database');
    throw error;
  }
}

export async function saveMessages({ messages }: { messages: Array<Message> }) {
  try {
    // return await db.insert(message).values(messages);
    return await Promise.all(messages
      .map(d => horus.createChatMessage({
        id: d.id,
        sessionId: d.chatId,
        role: d.role as string,
        content: d.content as string,
        queryEmbedding: null,
        referenceChunkIds:[],
        metadata: {}  
      })));
  } catch (error) {
    console.error('Failed to save messages in database', error);
    throw error;
  }
}

export async function getMessagesByChatId({ id }: { id: string }): Promise<Array<Message>> {
  try {
    // return await db
    //   .select()
    //   .from(message)
    //   .where(eq(message.chatId, id))
    //   .orderBy(asc(message.createdAt));

    var response = await horus.getChatSession(id);

    return response.messages.map(d => {
      return {
        id: d.id,
        chatId: id,
        role: d.role,
        content: d.content,
        createdAt: new Date(d.createdAt),
        sources: d.metadata['sources'] ?? null
      } as Message
    })
    .sort((d1, d2) => d1.createdAt.getTime() - d2.createdAt.getTime());
  } catch (error) {
    console.error('Failed to get messages by chat id from database', error);
    throw error;
  }
}

export async function voteMessage({
  chatId,
  messageId,
  type,
}: {
  chatId: string;
  messageId: string;
  type: 'up' | 'down';
}) {
  try {
    // const [existingVote] = await db
    //   .select()
    //   .from(vote)
    //   .where(and(eq(vote.messageId, messageId)));

    // if (existingVote) {
    //   return await db
    //     .update(vote)
    //     .set({ isUpvoted: type === 'up' })
    //     .where(and(eq(vote.messageId, messageId), eq(vote.chatId, chatId)));
    // }
    // return await db.insert(vote).values({
    //   chatId,
    //   messageId,
    //   isUpvoted: type === 'up',
    // });
    throw new Error('Not implemented');
  } catch (error) {
    console.error('Failed to upvote message in database', error);
    throw error;
  }
}

export async function getVotesByChatId({ id }: { id: string }) {
  try {
    // return await db.select().from(vote).where(eq(vote.chatId, id));
    return []
  } catch (error) {
    console.error('Failed to get votes by chat id from database', error);
    throw error;
  }
}

export async function saveDocument({
  id,
  title,
  kind,
  content,
  userId,
}: {
  id: string;
  title: string;
  kind: ArtifactKind;
  content: string;
  userId: string;
}) {
  try {
    // return await db.insert(document).values({
    //   id,
    //   title,
    //   kind,
    //   content,
    //   userId,
    //   createdAt: new Date(),
    // });
    throw new Error('Not implemented');
  } catch (error) {
    console.error('Failed to save document in database');
    throw error;
  }
}

export async function getDocumentsById({ id }: { id: string }) {
  try {
    // const documents = await db
    //   .select()
    //   .from(document)
    //   .where(eq(document.id, id))
    //   .orderBy(asc(document.createdAt));
    throw new Error('Not implemented');
  } catch (error) {
    console.error('Failed to get document by id from database');
    throw error;
  }
}

export async function getDocumentById({ id }: { id: string }) {
  try {
    // const [selectedDocument] = await db
    //   .select()
    //   .from(document)
    //   .where(eq(document.id, id))
    //   .orderBy(desc(document.createdAt));


    throw new Error('Not implemented');
  } catch (error) {
    console.error('Failed to get document by id from database');
    throw error;
  }
}

export async function deleteDocumentsByIdAfterTimestamp({
  id,
  timestamp,
}: {
  id: string;
  timestamp: Date;
}) {
  try {
    // await db
    //   .delete(suggestion)
    //   .where(
    //     and(
    //       eq(suggestion.documentId, id),
    //       gt(suggestion.documentCreatedAt, timestamp),
    //     ),
    //   );

    // return await db
    //   .delete(document)
    //   .where(and(eq(document.id, id), gt(document.createdAt, timestamp)));
    throw new Error('Not implemented');
  } catch (error) {
    console.error(
      'Failed to delete documents by id after timestamp from database',
    );
    throw error;
  }
}

export async function saveSuggestions({
  suggestions,
}: {
  suggestions: Array<Suggestion>;
}) {
  try {
    // return await db.insert(suggestion).values(suggestions);
   return []
  } catch (error) {
    console.error('Failed to save suggestions in database');
    throw error;
  }
}

export async function getSuggestionsByDocumentId({
  documentId,
}: {
  documentId: string;
}) {
  try {
    // return await db
    //   .select()
    //   .from(suggestion)
    //   .where(and(eq(suggestion.documentId, documentId)));
    return []
  } catch (error) {
    console.error(
      'Failed to get suggestions by document version from database',
    );
    throw error;
  }
}

export async function getMessageById({ id }: { id: string }) {
  try {
    // return await db.select().from(message).where(eq(message.id, id));
    throw new Error('Not implemented');
  } catch (error) {
    console.error('Failed to get message by id from database');
    throw error;
  }
}

export async function deleteMessagesByChatIdAfterTimestamp({
  chatId,
  timestamp,
}: {
  chatId: string;
  timestamp: Date;
}) {
  try {
    // const messagesToDelete = await db
    //   .select({ id: message.id })
    //   .from(message)
    //   .where(
    //     and(eq(message.chatId, chatId), gte(message.createdAt, timestamp)),
    //   );

    // const messageIds = messagesToDelete.map((message) => message.id);

    // if (messageIds.length > 0) {
    //   await db
    //     .delete(vote)
    //     .where(
    //       and(eq(vote.chatId, chatId), inArray(vote.messageId, messageIds)),
    //     );

    //   return await db
    //     .delete(message)
    //     .where(
    //       and(eq(message.chatId, chatId), inArray(message.id, messageIds)),
    //     );
    // }

    throw new Error('Not implemented');
  } catch (error) {
    console.error(
      'Failed to delete messages by id after timestamp from database',
    );
    throw error;
  }
}

export async function updateChatVisiblityById({
  chatId,
  visibility,
}: {
  chatId: string;
  visibility: 'private' | 'public';
}) {
  try {
    // return await db.update(chat).set({ visibility }).where(eq(chat.id, chatId));
    throw new Error('Not implemented');
  } catch (error) {
    console.error('Failed to update chat visibility in database');
    throw error;
  }
}
