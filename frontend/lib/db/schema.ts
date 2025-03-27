import { AssistantContent, ToolContent } from 'ai';
import type { InferSelectModel } from 'drizzle-orm';
import {
  pgTable,
  varchar,
  timestamp,
  json,
  uuid,
  text,
  primaryKey,
  foreignKey,
  boolean,
} from 'drizzle-orm/pg-core';



export type User = {id: string, email: string, password: string};

export type Chat = {
  id: string;
  createdAt: Date;
  title: string;
  userId: string;
  visibility: 'public' | 'private';
};


export type Message = {
  id: string;
  chatId: string;
  role: string;
  content: AssistantContent | ToolContent;
  createdAt: Date;
  reasoning?: string;
  experimental_attachments?: Attachment[];
  toolInvocations?: Array<ToolInvocation>;
  sources?: Source[]
}

export interface ToolCall<NAME extends string, ARGS> {
  /**
ID of the tool call. This ID is used to match the tool call with the tool result.
 */
  toolCallId: string;
  /**
Name of the tool that is being called.
 */
  toolName: NAME;
  /**
Arguments of the tool call. This is a JSON-serializable object that matches the tool's input schema.
   */
  args: ARGS;
}


export interface ToolResult<NAME extends string, ARGS, RESULT> {
  /**
ID of the tool call. This ID is used to match the tool call with the tool result.
   */
  toolCallId: string;
  /**
Name of the tool that was called.
   */
  toolName: NAME;
  /**
Arguments of the tool call. This is a JSON-serializable object that matches the tool's input schema.
     */
  args: ARGS;
  /**
Result of the tool call. This is the result of the tool's execution.
     */
  result: RESULT;
}

export type ToolInvocation = ({
  state: 'partial-call';
  step?: number;
} & ToolCall<string, any>) | ({
  state: 'call';
  step?: number;
} & ToolCall<string, any>) | ({
  state: 'result';
  step?: number;
} & ToolResult<string, any, any>);


export interface Attachment {
  /**
   * The name of the attachment, usually the file name.
   */
  name?: string;
  /**
   * A string indicating the [media type](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Type).
   * By default, it's extracted from the pathname's extension.
   */
  contentType?: string;
  /**
   * The URL of the attachment. It can either be a URL to a hosted file or a [Data URL](https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/Data_URLs).
   */
  url: string;
}

export interface Source {
  type: 'memory' | 'websearch' | 'chunk';
  content: string;
  documentName?: string;
  url?: string;
  createdAt: string;
}


export type Vote = {
  chatId: string;
  messageId: string;
  isUpvoted: boolean; 
}



export type Document = {
  id: string;
  createdAt: Date;
  title: string;
  content: string;
  kind: 'text' | 'code' | 'image' | 'sheet';
  userId: string;
}

export type Suggestion = {
  id: string;
  documentId: string;
  documentCreatedAt: Date;
  originalText: string;
  suggestedText: string;
  description: string;
  isResolved: boolean;
  userId: string;
  createdAt: Date;
}


// // Extendendo a interface Message para incluir sources
// export interface ExtendedMessage extends Message {
//   sources?: Source[];
// }