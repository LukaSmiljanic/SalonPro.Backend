import React, { useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { format, addDays, startOfDay, parseISO } from 'date-fns';
import { sr } from 'date-fns/locale';
import { useSearchParams } from 'react-router-dom';
import {
  Instagram, Sparkles, CalendarClock, Copy, Trash2, Pencil,
  CheckCircle2, Clock, AlertCircle, Link2, Crown, Unlink, ImageIcon,
} from 'lucide-react';
import { toast } from 'sonner';
import type { AxiosError } from 'axios';
import { useAuth } from '../hooks/useAuth';
import {
  deleteSocialPost,
  disconnectInstagram,
  ensureSocialSchema,
  generateSocialWeek,
  getInstagramConnectUrl,
  getInstagramOAuthResult,
  getInstagramStatus,
  getSocialDbDiagnostics,
  getSocialConfig,
  getSocialPosts,
  publishSocialPostNow,
  regeneratePostImage,
  scheduleSocialPost,
  updateSocialPost,
} from '../api/social';
import type { SocialPost, SocialPostStatus, SocialDbDiagnostics } from '../types';
import { queryKeys } from '../lib/queryKeys';
import { resolveSocialImageUrl } from '../lib/mediaUrl';
import { Button } from '../components/Button';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { EmptyState } from '../components/EmptyState';
import { SocialGallerySection } from '../components/SocialGallerySection';

const statusMeta: Record<SocialPostStatus, { label: string; className: string }> = {
  Draft: { label: 'Nacrt', className: 'bg-surface-2 text-text-muted' },
  Scheduled: { label: 'Zakazano', className: 'bg-primary-highlight text-primary' },
  Published: { label: 'Objavljeno', className: 'bg-success-bg text-success' },
  Failed: { label: 'Greška', className: 'bg-error-bg text-error' },
};

function toLocalInputValue(iso: string): string {
  const d = parseISO(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function fromLocalInputValue(value: string): string {
  return new Date(value).toISOString();
}

function apiErrorMessage(err: unknown, fallback: string): string {
  const ax = err as AxiosError<{ detail?: string; title?: string; message?: string }>;
  return ax.response?.data?.detail
    ?? ax.response?.data?.message
    ?? ax.response?.data?.title
    ?? ax.message
    ?? fallback;
}

export const SocialMarketingPage: React.FC = () => {
  const { features, plan } = useAuth();
  const canUse = features.canUseSocialMarketing === true || plan === 'Pro';
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();

  const weekStart = useMemo(() => startOfDay(new Date()), []);
  const weekEnd = useMemo(() => addDays(weekStart, 7), [weekStart]);
  const fromIso = weekStart.toISOString();
  const toIso = weekEnd.toISOString();

  const [editing, setEditing] = useState<SocialPost | null>(null);
  const [editCaption, setEditCaption] = useState('');
  const [editHashtags, setEditHashtags] = useState('');
  const [editScheduledAt, setEditScheduledAt] = useState('');

  const [igConnectError, setIgConnectError] = useState<string | null>(null);
  const [igConnecting, setIgConnecting] = useState(false);

  useEffect(() => {
    const ig = searchParams.get('instagram');
    if (ig === 'connected') {
      setIgConnectError(null);
      toast.success('Instagram nalog je povezan.');
      setSearchParams({}, { replace: true });
      queryClient.invalidateQueries({ queryKey: queryKeys.social.all });
    } else if (ig === 'connecting') {
      setIgConnectError(null);
      setIgConnecting(true);
      setSearchParams({}, { replace: true });
    } else if (ig === 'error') {
      const reason = searchParams.get('reason');
      const msg = reason
        ? decodeURIComponent(reason)
        : 'Proveri da imaš Facebook Page i da je Instagram Business povezan sa njom.';
      setIgConnectError(msg);
      toast.error(`Instagram: ${msg}`, { duration: 15_000 });
      setSearchParams({}, { replace: true });
    }
  }, [searchParams, setSearchParams, queryClient]);

  useEffect(() => {
    if (!igConnecting) return;

    let cancelled = false;

    const poll = async () => {
      try {
        const result = await getInstagramOAuthResult();
        if (cancelled) return;
        if (result.status === 'Connected') {
          setIgConnecting(false);
          toast.success('Instagram nalog je povezan.');
          queryClient.invalidateQueries({ queryKey: queryKeys.social.all });
        } else if (result.status === 'Error') {
          setIgConnecting(false);
          const msg = result.message ?? 'Povezivanje nije uspelo.';
          setIgConnectError(msg);
          toast.error(`Instagram: ${msg}`, { duration: 15_000 });
        }
      } catch {
        // keep polling until timeout
      }
    };

    void poll();
    const interval = window.setInterval(() => void poll(), 2500);
    const timeout = window.setTimeout(() => {
      if (cancelled) return;
      setIgConnecting(false);
      const msg = 'Povezivanje traje predugo. Proveri Facebook Page i pokušaj ponovo.';
      setIgConnectError(msg);
      toast.error(msg);
    }, 90_000);

    return () => {
      cancelled = true;
      window.clearInterval(interval);
      window.clearTimeout(timeout);
    };
  }, [igConnecting, queryClient]);

  const { data: igStatus } = useQuery({
    queryKey: [...queryKeys.social.all, 'instagram-status'],
    queryFn: getInstagramStatus,
    enabled: canUse,
  });

  const { data: socialConfig } = useQuery({
    queryKey: [...queryKeys.social.all, 'config'],
    queryFn: getSocialConfig,
    enabled: canUse,
    staleTime: 30_000,
  });

  const [pendingImageIds, setPendingImageIds] = useState<Set<string>>(new Set());
  const [imageCacheBust, setImageCacheBust] = useState<Record<string, number>>({});
  const isPollingImages = pendingImageIds.size > 0;

  const { data, isLoading, error, refetch, isFetching } = useQuery({
    queryKey: queryKeys.social.week(fromIso, toIso),
    queryFn: () => getSocialPosts(fromIso, toIso),
    enabled: canUse,
    refetchInterval: isPollingImages ? 3000 : false,
    refetchIntervalInBackground: true,
  });

  useEffect(() => {
    if (!data?.posts || pendingImageIds.size === 0) return;

    const postsById = new Map(data.posts.map((p) => [p.id, p]));
    const stillPending = new Set<string>();
    let completed = 0;
    const newBust: Record<string, number> = {};

    for (const id of pendingImageIds) {
      const post = postsById.get(id);
      if (!post) {
        stillPending.add(id);
        continue;
      }
      if (post.failureReason) {
        toast.error(post.failureReason);
        continue;
      }
      const resolved = resolveSocialImageUrl(post.imageUrl);
      if (resolved) {
        completed += 1;
        newBust[id] = Date.now();
        continue;
      }
      stillPending.add(id);
    }

    if (completed > 0) {
      setImageCacheBust((prev) => ({ ...prev, ...newBust }));
      toast.success(completed === 1 ? 'Slika je spremna.' : `${completed} slike su spremne.`);
    }

    if (stillPending.size !== pendingImageIds.size) {
      setPendingImageIds(stillPending);
    }
  }, [data, pendingImageIds]);

  // Stop polling after 3 minutes and warn
  useEffect(() => {
    if (!isPollingImages) return;
    const timer = window.setTimeout(() => {
      setPendingImageIds((prev) => {
        if (prev.size > 0) {
          toast.error('Generisanje slike traje predugo. Proverite OpenAI podešavanja ili pokušajte ponovo.');
        }
        return new Set();
      });
    }, 180_000);
    return () => window.clearTimeout(timer);
  }, [isPollingImages, pendingImageIds.size]);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: queryKeys.social.all });
  };

  const generateMutation = useMutation({
    mutationFn: () => generateSocialWeek(format(weekStart, 'yyyy-MM-dd')),
    onSuccess: (result) => {
      const withoutImages = result.posts.filter((p) => !p.imageUrl).map((p) => p.id);
      if (withoutImages.length > 0) {
        setPendingImageIds((prev) => new Set([...prev, ...withoutImages]));
        toast.success(`Generisano ${result.posts.length} predloga. Slike se generišu u pozadini (30–90 sek).`);
      } else {
        toast.success(`Generisano ${result.posts.length} predloga.`);
      }
      invalidate();
    },
    onError: (err) => toast.error(apiErrorMessage(err, 'Generisanje nije uspelo.')),
  });

  const ensureSchemaMutation = useMutation({
    mutationFn: ensureSocialSchema,
    onSuccess: (result) => {
      if (result.success) {
        toast.success('Tabele za marketing su kreirane.');
        invalidate();
        refetch();
      } else {
        toast.error(result.steps.join(' | '));
      }
    },
    onError: (err) => toast.error(apiErrorMessage(err, 'Kreiranje tabela nije uspelo.')),
  });

  const [dbDiag, setDbDiag] = useState<SocialDbDiagnostics | null>(null);

  const schemaError = useMemo(() => {
    const ax = error as AxiosError<{ detail?: string }> | null;
    return ax?.response?.status === 503
      || (ax?.response?.data?.detail?.includes('SocialPosts') ?? false);
  }, [error]);

  const fetchDbDiagnostics = async () => {
    try {
      const d = await getSocialDbDiagnostics();
      setDbDiag(d);
      return d;
    } catch {
      return null;
    }
  };

  useEffect(() => {
    if (schemaError) fetchDbDiagnostics();
  }, [schemaError]);

  const connectMutation = useMutation({
    mutationFn: getInstagramConnectUrl,
    onSuccess: ({ url }) => {
      window.location.href = url;
    },
    onError: () => toast.error('Meta aplikacija nije konfigurisana na serveru.'),
  });

  const disconnectMutation = useMutation({
    mutationFn: disconnectInstagram,
    onSuccess: () => {
      toast.success('Instagram veza uklonjena.');
      invalidate();
    },
  });

  const updateMutation = useMutation({
    mutationFn: () => {
      if (!editing) throw new Error('no post');
      return updateSocialPost(editing.id, {
        caption: editCaption,
        hashtags: editHashtags,
        scheduledAt: fromLocalInputValue(editScheduledAt),
      });
    },
    onSuccess: () => {
      toast.success('Objava sačuvana.');
      setEditing(null);
      invalidate();
    },
    onError: () => toast.error('Čuvanje nije uspelo.'),
  });

  const scheduleMutation = useMutation({
    mutationFn: scheduleSocialPost,
    onSuccess: () => {
      toast.success('Objava zakazana.');
      invalidate();
    },
    onError: () => toast.error('Zakazivanje nije uspelo.'),
  });

  const publishNowMutation = useMutation({
    mutationFn: publishSocialPostNow,
    onSuccess: (result) => {
      invalidate();
      if (result.success) {
        toast.success('Objava je objavljena na Instagramu.');
      } else {
        toast.error(result.failureReason ?? 'Objava nije uspela.', { duration: 15_000 });
      }
    },
    onError: (err) => toast.error(apiErrorMessage(err, 'Objava nije uspela.'), { duration: 15_000 }),
  });

  const deleteMutation = useMutation({
    mutationFn: deleteSocialPost,
    onSuccess: () => {
      toast.success('Objava obrisana.');
      invalidate();
    },
    onError: () => toast.error('Brisanje nije uspelo.'),
  });

  const [regeneratingId, setRegeneratingId] = useState<string | null>(null);

  const regenerateMutation = useMutation({
    mutationFn: async (id: string) => {
      setRegeneratingId(id);
      setPendingImageIds((prev) => new Set(prev).add(id));
      try {
        return await regeneratePostImage(id);
      } catch (err) {
        setPendingImageIds((prev) => {
          const next = new Set(prev);
          next.delete(id);
          return next;
        });
        throw err;
      } finally {
        setRegeneratingId(null);
      }
    },
    onSuccess: (result) => {
      toast.info(result.message || 'Slika se generiše u pozadini…');
      invalidate();
    },
    onError: (err) => toast.error(apiErrorMessage(err, 'Pokretanje generisanja slike nije uspelo.')),
  });

  const openEdit = (post: SocialPost) => {
    setEditing(post);
    setEditCaption(post.caption);
    setEditHashtags(post.hashtags);
    setEditScheduledAt(toLocalInputValue(post.scheduledAt));
  };

  const copyPost = async (post: SocialPost) => {
    const text = `${post.caption}\n\n${post.hashtags}`;
    await navigator.clipboard.writeText(text);
    toast.success('Tekst kopiran.');
  };

  const instagramConnected = igStatus?.isConnected ?? data?.instagramConnected ?? false;
  const instagramUsername = igStatus?.username ?? data?.instagramUsername;
  const openAiHasKey = socialConfig?.hasApiKeyInConfig;
  const lastOpenAiError = socialConfig?.lastOpenAiError;

  if (!canUse) {
    return (
      <div className="page-container max-w-lg mx-auto text-center py-16">
        <div className="w-14 h-14 rounded-2xl bg-primary-highlight flex items-center justify-center mx-auto mb-4">
          <Crown size={28} className="text-primary" />
        </div>
        <h1 className="text-xl font-semibold text-display text-text mb-2">Marketing (Pro)</h1>
        <p className="text-sm text-text-muted mb-6">
          AI planer Instagram objava dostupan je u <strong>Pro</strong> paketu.
        </p>
        <Button variant="secondary" onClick={() => window.location.href = '/settings'}>
          Podešavanja pretplate
        </Button>
      </div>
    );
  }

  return (
    <div className="page-container space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <Instagram size={22} className="text-primary" />
            <h1 className="text-xl font-semibold text-display text-text">Instagram marketing</h1>
            <span className="text-[10px] font-semibold uppercase tracking-wider px-2 py-0.5 rounded-full bg-warning-bg text-warning">
              Beta
            </span>
          </div>
          <p className="text-sm text-text-muted max-w-xl">
            OpenAI generiše tekst i slike za 7 dana. Zakazite objave — automatski publish na povezan Instagram.
          </p>
        </div>
        <div className="flex flex-wrap gap-2 shrink-0">
          <Button
            variant="secondary"
            size="sm"
            onClick={() => refetch()}
            disabled={isFetching}
          >
            Osveži
          </Button>
          <Button
            size="sm"
            icon={<Sparkles size={14} />}
            onClick={() => generateMutation.mutate()}
            disabled={generateMutation.isPending}
          >
            {generateMutation.isPending ? 'AI radi…' : 'Generiši 7 dana'}
          </Button>
        </div>
      </div>

      {socialConfig && !socialConfig.openAiConfigured && (
        <div className="flex items-start gap-3 p-3 rounded-lg border border-warning/30 bg-warning-bg text-sm">
          <AlertCircle size={16} className="text-warning shrink-0 mt-0.5" />
          <div>
            <p className="font-medium text-text">OpenAI nije spreman na serveru</p>
            <p className="text-text-muted text-xs mt-0.5">
              {openAiHasKey === false && (
                <>
                  API ne vidi ključ. Na MonsterASP u <code className="text-xs">appsettings.json</code> na serveru
                  postavi <code className="text-xs">OpenAI:ApiKey</code> ili env varijablu{' '}
                  <code className="text-xs">OpenAI__ApiKey</code>, pa restartuj aplikaciju.
                  <br />
                  <strong>Napomena:</strong> svaki publish iz Visual Studio-a više neće brisati ključ (ispravljeno u kodu).
                </>
              )}
              {openAiHasKey === true && !socialConfig.openAiEnabled && (
                <>OpenAI je isključen (<code className="text-xs">OpenAI:Enabled</code> = false).</>
              )}
              {lastOpenAiError && (
                <span className="block mt-1 text-error">Poslednja greška: {lastOpenAiError}</span>
              )}
            </p>
          </div>
        </div>
      )}

      {igConnectError && (
        <div className="p-3 rounded-lg border border-error/30 bg-error-bg text-error text-sm flex items-start gap-2">
          <AlertCircle size={16} className="shrink-0 mt-0.5" />
          <div>
            <p className="font-medium">Instagram povezivanje nije uspelo</p>
            <p className="mt-1">{igConnectError}</p>
          </div>
        </div>
      )}

      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 p-3 rounded-lg border border-border bg-surface-2 text-sm">
        <div className="flex items-start gap-3">
          <Link2 size={16} className="text-text-faint shrink-0 mt-0.5" />
          <div>
            {instagramConnected ? (
              <>
                <p className="font-medium text-text flex items-center gap-1">
                  <CheckCircle2 size={14} className="text-success" />
                  Povezano: @{instagramUsername ?? 'instagram'}
                </p>
                <p className="text-text-muted text-xs mt-0.5">
                  Zakazane objave će se objavljivati na ovaj nalog.
                </p>
              </>
            ) : (
              <>
                <p className="font-medium text-text">Instagram nije povezan</p>
                <p className="text-text-muted text-xs mt-0.5">
                  Potreban Instagram Business nalog povezan sa Facebook stranicom.
                </p>
              </>
            )}
          </div>
        </div>
        {instagramConnected ? (
          <Button
            variant="secondary"
            size="sm"
            icon={<Unlink size={14} />}
            onClick={() => disconnectMutation.mutate()}
            disabled={disconnectMutation.isPending}
          >
            Prekini vezu
          </Button>
        ) : (
          <Button
            size="sm"
            icon={<Instagram size={14} />}
            onClick={() => connectMutation.mutate()}
            disabled={connectMutation.isPending || igConnecting}
          >
            {igConnecting ? 'Povezivanje…' : 'Poveži Instagram'}
          </Button>
        )}
      </div>

      {igConnecting && (
        <div className="flex items-center gap-2 p-3 rounded-lg border border-primary/20 bg-primary-highlight text-sm text-primary">
          <LoadingSpinner size="sm" />
          Povezujemo Instagram sa Facebook stranicom…
        </div>
      )}

      <SocialGallerySection
        onVariantQueued={(postId) => {
          setPendingImageIds((prev) => new Set(prev).add(postId));
        }}
      />

      {generateMutation.isPending && (
        <div className="flex items-center gap-2 p-3 rounded-lg border border-primary/20 bg-primary-highlight text-sm text-primary">
          <LoadingSpinner size="sm" />
          Generisanje može trajati 1–3 min (7 tekstova + slike)…
        </div>
      )}

      {isLoading ? (
        <div className="flex justify-center py-16">
          <LoadingSpinner />
        </div>
      ) : error ? (
        <div className="text-center py-8 space-y-3">
          <p className="text-sm text-error">
            {apiErrorMessage(error, 'Nije moguće učitati objave.')}
          </p>
          {schemaError && (
            <div className="max-w-lg mx-auto text-left p-3 rounded-lg border border-warning/30 bg-warning-bg text-xs text-text-muted space-y-2">
              <p>OpenAI radi — problem je samo baza. API mora videti tabele u bazi <strong>db43760</strong>.</p>
              {dbDiag && (
                <div className="font-mono text-[11px] bg-surface p-2 rounded border border-border">
                  <div>Baza koju API koristi: <strong>{dbDiag.databaseName}</strong> @ {dbDiag.serverName}</div>
                  <div>SocialPosts: {dbDiag.socialPostsTableExists ? 'postoji' : 'NE POSTOJI'}</div>
                  {dbDiag.socialPostsQueryError && <div className="text-error">{dbDiag.socialPostsQueryError}</div>}
                  {dbDiag.socialPostsColumns.length > 0 && (
                    <div>Kolone: {dbDiag.socialPostsColumns.join(', ')}</div>
                  )}
                </div>
              )}
              <Button
                size="sm"
                onClick={() => ensureSchemaMutation.mutate()}
                disabled={ensureSchemaMutation.isPending}
              >
                {ensureSchemaMutation.isPending ? 'Kreiram tabele…' : 'Kreiraj marketing tabele (API)'}
              </Button>
            </div>
          )}
        </div>
      ) : !data?.posts.length ? (
        <EmptyState
          title="Nema planiranih objava"
          description="Kliknite „Generiši 7 dana” — OpenAI kreira tekst i DALL-E slike na osnovu vašeg salona."
          action={
            <Button
              icon={<Sparkles size={14} />}
              onClick={() => generateMutation.mutate()}
              disabled={generateMutation.isPending}
            >
              Generiši 7 dana
            </Button>
          }
        />
      ) : (
        <div className="grid gap-3 lg:grid-cols-2">
          {data.posts.map((post) => {
            const st = statusMeta[post.status];
            const imageSrc = resolveSocialImageUrl(post.imageUrl);
            const isGenerating = pendingImageIds.has(post.id);
            const displaySrc = imageSrc
              ? `${imageSrc}${imageSrc.includes('?') ? '&' : '?'}v=${imageCacheBust[post.id] ?? 0}`
              : null;
            return (
              <article key={post.id} className="card card-padded">
                <div className="flex flex-wrap items-start justify-between gap-2 mb-3">
                  <div>
                    <p className="text-sm font-semibold text-text">{post.topic}</p>
                    <p className="text-xs text-text-faint flex items-center gap-1 mt-0.5">
                      <CalendarClock size={12} />
                      {format(parseISO(post.scheduledAt), "EEEE d. MMM 'u' HH:mm", { locale: sr })}
                    </p>
                  </div>
                  <span className={`text-[10px] font-semibold uppercase px-2 py-0.5 rounded-full ${st.className}`}>
                    {st.label}
                  </span>
                </div>

                {displaySrc && !isGenerating ? (
                  <img
                    src={displaySrc}
                    alt={post.topic}
                    className="w-full aspect-square object-cover rounded-lg mb-3 border border-border"
                    loading="lazy"
                    onError={(e) => {
                      (e.target as HTMLImageElement).style.display = 'none';
                      toast.error('Slika postoji na serveru ali se ne može učitati. Proverite deploy i _redirects na Netlify.');
                    }}
                  />
                ) : isGenerating ? (
                  <div className="w-full aspect-square rounded-lg mb-3 border border-border bg-surface-2 flex flex-col items-center justify-center gap-2 text-text-muted text-xs">
                    <LoadingSpinner size="md" />
                    <span>AI generiše sliku…</span>
                  </div>
                ) : (
                  <div className="w-full aspect-square rounded-lg mb-3 border border-dashed border-border bg-surface-2 flex flex-col items-center justify-center gap-2 text-text-faint text-xs">
                    <ImageIcon size={28} />
                    <span>Nema slike</span>
                  </div>
                )}

                <p className="text-sm text-text-muted whitespace-pre-line line-clamp-4 mb-2">{post.caption}</p>
                <p className="text-xs text-primary/80 line-clamp-2 mb-3">{post.hashtags}</p>

                {post.failureReason && (
                  <p className="text-xs text-error flex items-center gap-1 mb-3">
                    <AlertCircle size={12} /> {post.failureReason}
                  </p>
                )}

                <div className="flex flex-wrap gap-2 pt-2 border-t border-border">
                  {post.status !== 'Published' && (
                    <>
                      <Button variant="secondary" size="sm" icon={<Pencil size={12} />} onClick={() => openEdit(post)}>
                        Uredi
                      </Button>
                      {post.status === 'Draft' && (
                        <Button
                          size="sm"
                          icon={<Clock size={12} />}
                          onClick={() => scheduleMutation.mutate(post.id)}
                          disabled={scheduleMutation.isPending}
                        >
                          Zakazi
                        </Button>
                      )}
                      {post.status === 'Scheduled' && (
                        <Button
                          size="sm"
                          icon={<Instagram size={12} />}
                          onClick={() => publishNowMutation.mutate(post.id)}
                          disabled={publishNowMutation.isPending}
                        >
                          Objavi sada
                        </Button>
                      )}
                      <Button
                        variant="secondary"
                        size="sm"
                        icon={<ImageIcon size={12} />}
                        onClick={() => regenerateMutation.mutate(post.id)}
                        disabled={regeneratingId !== null || pendingImageIds.has(post.id)}
                      >
                        {regeneratingId === post.id || pendingImageIds.has(post.id) ? 'AI slika…' : 'Nova slika'}
                      </Button>
                      <Button
                        variant="secondary"
                        size="sm"
                        icon={<Trash2 size={12} />}
                        onClick={() => deleteMutation.mutate(post.id)}
                        disabled={deleteMutation.isPending}
                      >
                        Obriši
                      </Button>
                    </>
                  )}
                  <Button variant="secondary" size="sm" icon={<Copy size={12} />} onClick={() => copyPost(post)}>
                    Kopiraj
                  </Button>
                  {post.status === 'Published' && (
                    <span className="text-xs text-success flex items-center gap-1 ml-auto">
                      <CheckCircle2 size={14} />
                      {post.publishedAt
                        ? format(parseISO(post.publishedAt), 'd.M. HH:mm', { locale: sr })
                        : 'Objavljeno'}
                    </span>
                  )}
                </div>
              </article>
            );
          })}
        </div>
      )}

      {editing && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
          <div className="card card-padded w-full max-w-lg max-h-[90vh] overflow-y-auto shadow-xl">
            <h2 className="text-lg font-semibold text-text mb-4">Uredi objavu</h2>
            <div className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-text-muted mb-1">Tekst</label>
                <textarea
                  className="input w-full min-h-[140px] resize-y"
                  value={editCaption}
                  onChange={(e) => setEditCaption(e.target.value)}
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-text-muted mb-1">Hashtagovi</label>
                <textarea
                  className="input w-full min-h-[60px] resize-y"
                  value={editHashtags}
                  onChange={(e) => setEditHashtags(e.target.value)}
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-text-muted mb-1">Vreme objave</label>
                <input
                  type="datetime-local"
                  className="input w-full"
                  value={editScheduledAt}
                  onChange={(e) => setEditScheduledAt(e.target.value)}
                />
              </div>
            </div>
            <div className="flex justify-end gap-2 mt-6">
              <Button variant="secondary" onClick={() => setEditing(null)}>Otkaži</Button>
              <Button onClick={() => updateMutation.mutate()} disabled={updateMutation.isPending}>
                Sačuvaj
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
