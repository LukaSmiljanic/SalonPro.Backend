import React, { useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ImagePlus, Sparkles, Trash2, Upload, Wand2 } from 'lucide-react';
import { toast } from 'sonner';
import type { AxiosError } from 'axios';
import {
  createGalleryAiVariant,
  createPostFromGallery,
  deleteSocialGalleryImage,
  getSocialGallery,
  uploadSocialGalleryImage,
} from '../api/social';
import { queryKeys } from '../lib/queryKeys';
import { resolveSocialImageUrl } from '../lib/mediaUrl';
import type { SocialGalleryImage } from '../types';
import { Button } from './Button';
import { LoadingSpinner } from './LoadingSpinner';
import { Modal } from './Modal';

function apiErrorMessage(err: unknown, fallback: string): string {
  const ax = err as AxiosError<{ detail?: string; title?: string; message?: string }>;
  return ax.response?.data?.detail
    ?? ax.response?.data?.message
    ?? ax.response?.data?.title
    ?? ax.message
    ?? fallback;
}

export const SocialGallerySection: React.FC<{
  onVariantQueued?: (postId: string) => void;
}> = ({ onVariantQueued }) => {
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploadHint, setUploadHint] = useState('');
  const [creatingId, setCreatingId] = useState<string | null>(null);
  const [variantImage, setVariantImage] = useState<SocialGalleryImage | null>(null);
  const [variantPrompt, setVariantPrompt] = useState('');
  const [variantHint, setVariantHint] = useState('');
  const [uploadProgress, setUploadProgress] = useState<string | null>(null);

  const { data: images = [], isLoading } = useQuery({
    queryKey: queryKeys.social.gallery(),
    queryFn: getSocialGallery,
  });

  const uploadMutation = useMutation({
    mutationFn: async (files: File[]) => {
      let uploaded = 0;
      for (const file of files) {
        setUploadProgress(`${uploaded + 1}/${files.length}`);
        await uploadSocialGalleryImage(file, uploadHint);
        uploaded++;
      }
      return uploaded;
    },
    onSuccess: (count) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.social.gallery() });
      setUploadHint('');
      setUploadProgress(null);
      if (fileInputRef.current) fileInputRef.current.value = '';
      toast.success(count === 1 ? 'Slika je dodata u galeriju.' : `${count} slike su dodate u galeriju.`);
    },
    onError: (err) => {
      setUploadProgress(null);
      toast.error(apiErrorMessage(err, 'Upload nije uspeo.'));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteSocialGalleryImage,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.social.gallery() });
      toast.success('Slika je obrisana.');
    },
    onError: (err) => toast.error(apiErrorMessage(err, 'Brisanje nije uspelo.')),
  });

  const createPostMutation = useMutation({
    mutationFn: ({ id, hint }: { id: string; hint?: string }) => createPostFromGallery(id, hint),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.social.all });
      toast.success('Nacrt objave je kreiran — pogledajte listu ispod.');
      setCreatingId(null);
    },
    onError: (err) => {
      setCreatingId(null);
      toast.error(apiErrorMessage(err, 'Kreiranje objave nije uspelo.'));
    },
  });

  const aiVariantMutation = useMutation({
    mutationFn: ({ id, prompt, captionHint }: { id: string; prompt: string; captionHint?: string }) =>
      createGalleryAiVariant(id, { prompt, captionHint: captionHint ?? null }),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.social.all });
      onVariantQueued?.(result.post.id);
      toast.info(result.message || 'AI varijanta se generiše u pozadini…');
      setVariantImage(null);
      setVariantPrompt('');
      setVariantHint('');
    },
    onError: (err) => toast.error(apiErrorMessage(err, 'AI varijanta nije uspela.')),
  });

  const onFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? []);
    if (files.length === 0) return;
    uploadMutation.mutate(files);
  };

  const openVariantModal = (img: SocialGalleryImage) => {
    setVariantImage(img);
    setVariantPrompt('');
    setVariantHint(img.captionHint ?? '');
  };

  const submitVariant = () => {
    if (!variantImage || !variantPrompt.trim()) return;
    aiVariantMutation.mutate({
      id: variantImage.id,
      prompt: variantPrompt.trim(),
      captionHint: variantHint.trim() || undefined,
    });
  };

  return (
    <>
      <div className="card card-padded space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <ImagePlus size={18} className="text-primary" />
              <h2 className="text-base font-semibold text-text">Galerija salona</h2>
            </div>
            <p className="text-sm text-text-muted">
              Uploadujte fotografije (više odjednom). AI piše tekst uz sliku, ili pravi novu varijantu koja zadržava brend i dizajn.
            </p>
          </div>
          <div className="flex flex-col sm:items-end gap-2 shrink-0">
            <input
              ref={fileInputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              multiple
              className="hidden"
              onChange={onFileChange}
            />
            <Button
              size="sm"
              icon={<Upload size={14} />}
              onClick={() => fileInputRef.current?.click()}
              loading={uploadMutation.isPending}
            >
              {uploadProgress ? `Upload ${uploadProgress}` : 'Dodaj slike'}
            </Button>
          </div>
        </div>

        <div>
          <label className="block text-xs font-medium text-text-muted mb-1">
            Kontekst za AI (opciono, pri uploadu)
          </label>
          <input
            type="text"
            className="input w-full"
            placeholder="npr. Loreal bočica, nova frizura, enterijer salona…"
            value={uploadHint}
            onChange={(e) => setUploadHint(e.target.value)}
          />
        </div>

        {isLoading ? (
          <div className="flex justify-center py-8">
            <LoadingSpinner />
          </div>
        ) : images.length === 0 ? (
          <p className="text-sm text-text-faint text-center py-6 border border-dashed border-border rounded-lg">
            Još nema slika — dodajte prvu fotografiju iz salona.
          </p>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
            {images.map((img) => {
              const src = resolveSocialImageUrl(img.imageUrl);
              return (
                <article key={img.id} className="rounded-lg border border-border overflow-hidden bg-surface">
                  {src ? (
                    <img src={src} alt={img.fileName} className="w-full aspect-square object-cover" loading="lazy" />
                  ) : (
                    <div className="w-full aspect-square bg-surface-2" />
                  )}
                  <div className="p-2 space-y-2">
                    <p className="text-[11px] text-text-faint truncate" title={img.fileName}>{img.fileName}</p>
                    <div className="flex flex-wrap gap-1">
                      <Button
                        size="sm"
                        className="flex-1 min-w-0"
                        icon={<Sparkles size={12} />}
                        onClick={() => {
                          setCreatingId(img.id);
                          createPostMutation.mutate({ id: img.id, hint: img.captionHint ?? undefined });
                        }}
                        disabled={createPostMutation.isPending || aiVariantMutation.isPending}
                      >
                        {creatingId === img.id ? 'AI…' : 'Objava'}
                      </Button>
                      <Button
                        size="sm"
                        variant="secondary"
                        className="flex-1 min-w-0"
                        icon={<Wand2 size={12} />}
                        onClick={() => openVariantModal(img)}
                        disabled={createPostMutation.isPending || aiVariantMutation.isPending}
                      >
                        Varijanta
                      </Button>
                      <button
                        type="button"
                        onClick={() => deleteMutation.mutate(img.id)}
                        disabled={deleteMutation.isPending}
                        className="p-2 rounded-md text-text-muted hover:text-error hover:bg-error-bg transition-colors"
                        title="Obriši"
                      >
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </div>

      <Modal
        isOpen={variantImage !== null}
        onClose={() => {
          if (!aiVariantMutation.isPending) {
            setVariantImage(null);
            setVariantPrompt('');
            setVariantHint('');
          }
        }}
        title="AI varijanta slike"
        footer={(
          <>
            <Button
              variant="secondary"
              onClick={() => setVariantImage(null)}
              disabled={aiVariantMutation.isPending}
            >
              Otkaži
            </Button>
            <Button
              icon={<Wand2 size={14} />}
              onClick={submitVariant}
              loading={aiVariantMutation.isPending}
              disabled={!variantPrompt.trim()}
            >
              Generiši
            </Button>
          </>
        )}
      >
        <div className="space-y-4">
          <p className="text-sm text-text-muted">
            AI pravi novu sliku na osnovu izabrane fotografije. Brend, logo i dizajn proizvoda ostaju isti — menjate pozadinu, svetlo, ugao i sl.
          </p>
          {variantImage && (
            <div className="rounded-lg overflow-hidden border border-border max-w-[200px]">
              <img
                src={resolveSocialImageUrl(variantImage.imageUrl) ?? ''}
                alt={variantImage.fileName}
                className="w-full aspect-square object-cover"
              />
            </div>
          )}
          <div>
            <label className="block text-xs font-medium text-text-muted mb-1">
              Šta želite da promenite?
            </label>
            <textarea
              className="input w-full min-h-[88px]"
              placeholder="npr. mekše svetlo, mermerna pozadina, blagi bokeh, Instagram format…"
              value={variantPrompt}
              onChange={(e) => setVariantPrompt(e.target.value)}
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-text-muted mb-1">
              Kontekst za caption (opciono)
            </label>
            <input
              type="text"
              className="input w-full"
              placeholder="npr. promocija Loreal linije, nova kolekcija noktiju…"
              value={variantHint}
              onChange={(e) => setVariantHint(e.target.value)}
            />
          </div>
          {aiVariantMutation.isPending && (
            <p className="text-xs text-text-muted">Pokretanje generisanja…</p>
          )}
        </div>
      </Modal>
    </>
  );
};
