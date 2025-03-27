import { Button } from './ui/button';
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from './ui/alert-dialog';
import { cn } from '@/lib/utils';
import { Source } from '@/lib/db/schema';


interface MessageSourcesProps {
  sources: Source[];
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  onClose: () => void;
  highlightedSourceIndex?: number;
}

const formatRelativeTime = (timestamp: string) => {
  const date = new Date(timestamp);
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (diffInSeconds < 60) return 'now';
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)} minutes ago`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)} hours ago`;
  if (diffInSeconds < 604800) return `${Math.floor(diffInSeconds / 86400)} days ago`;
  return date.toLocaleDateString();
};



export const MessageSources = ({
  sources,
  isOpen,
  onOpenChange,
  onClose,
  highlightedSourceIndex,
}: MessageSourcesProps) => {
  const groupedSources = sources.reduce((acc, source) => {
    if (!acc[source.type]) {
      acc[source.type] = [];
    }
    acc[source.type].push(source);
    return acc;
  }, {} as Record<string, Source[]>);

  const originalSources = sources;

  return (
    <div className="mt-2">
      <AlertDialog open={isOpen} onOpenChange={onOpenChange}>
        
        <AlertDialogTrigger asChild>
          <Button variant="link" className="text-muted-foreground p-0 h-auto">
            See sources ({sources.length})
          </Button>
        </AlertDialogTrigger>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Sources</AlertDialogTitle>
          </AlertDialogHeader>
          <div className="space-y-4">
            {Object.entries(groupedSources).map(([type, sources]) => (
              <div key={type} className="mb-4">
                <h3 className="text-lg font-semibold capitalize">
                  {type === 'memory' ? 'Memory' : type === 'websearch' ? 'Web' : 'Documents'}
                </h3>
                {sources.map((source, index) => {
                const globalIndex = originalSources.indexOf(source);
                const isHighlighted = globalIndex == highlightedSourceIndex;

                  return (
                    <div
                      key={index}
                      className={cn('text-sm mt-1', {
                        'bg-yellow-200 p-2 rounded text-black ': isHighlighted,
                      })}
                    >
                      <strong>{source.content}</strong>
                      {source.url && (
                        <div>
                          <a
                            href={source.url}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-blue-500 underline"
                          >
                            Link da fonte
                          </a>
                        </div>
                      )}
                      {source.documentName && <div>Documento: {source.documentName}</div>}
                      <div className="text-xs text-gray-500">
                        {formatRelativeTime(source.createdAt)}
                      </div>
                    </div>
                  );
                })}
              </div>
            ))}
          </div>
          <AlertDialogFooter>
            <AlertDialogCancel onClick={onClose}>Close</AlertDialogCancel>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
};