import { useState } from 'react'
import { api } from '../api/client'
import type { PostDto, UserSummary } from '../api/types'
import { firstName, PRIVACY } from '../lib/format'
import { Avatar } from './Avatar'
import { Icon } from './Icon'

interface ComposerProps {
  user: UserSummary
  onCreated: (post: PostDto) => void
}

export function Composer({ user, onCreated }: ComposerProps) {
  const [open, setOpen] = useState(false)
  const [content, setContent] = useState('')
  const [imageUrl, setImageUrl] = useState('')
  const [showImageField, setShowImageField] = useState(false)
  const [privacy, setPrivacy] = useState(0)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function reset() {
    setContent('')
    setImageUrl('')
    setShowImageField(false)
    setPrivacy(0)
    setError(null)
  }

  function close() {
    if (busy) return
    setOpen(false)
    reset()
  }

  async function submit() {
    const text = content.trim()
    const image = imageUrl.trim()
    if (!text && !image) {
      setError('Write something or add an image.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      const created = await api.createPost({ content: text, imageUrl: image || null, privacy })
      onCreated(created)
      setOpen(false)
      reset()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not publish your post.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="card composer">
      <div className="composer-top">
        <Avatar name={user.displayName} src={user.avatarUrl} size={40} />
        <button type="button" className="composer-trigger" onClick={() => setOpen(true)}>
          What&apos;s on your mind, {firstName(user.displayName)}?
        </button>
      </div>
      <div className="composer-divider" />
      <div className="composer-shortcuts">
        <button type="button" onClick={() => setOpen(true)}>
          <Icon name="video" size={22} className="ic-live" />
          <span>Live video</span>
        </button>
        <button
          type="button"
          onClick={() => {
            setShowImageField(true)
            setOpen(true)
          }}
        >
          <Icon name="photo" size={22} className="ic-photo" />
          <span>Photo/video</span>
        </button>
        <button type="button" onClick={() => setOpen(true)}>
          <Icon name="feeling" size={22} className="ic-feeling" />
          <span>Feeling/activity</span>
        </button>
      </div>

      {open && (
        <div className="modal-backdrop" role="presentation" onClick={close}>
          <div className="modal composer-modal" role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
            <header className="modal-head">
              <h2>Create post</h2>
              <button type="button" className="icon-circle subtle" onClick={close} aria-label="Close">
                <Icon name="close" size={20} />
              </button>
            </header>
            <div className="modal-body">
              <div className="composer-identity">
                <Avatar name={user.displayName} src={user.avatarUrl} size={40} />
                <div>
                  <strong>{user.displayName}</strong>
                  <label className="privacy-pill">
                    <Icon name={PRIVACY[privacy]?.icon ?? 'globe'} size={13} />
                    <select value={privacy} onChange={(e) => setPrivacy(Number(e.target.value))}>
                      {PRIVACY.map((p) => (
                        <option key={p.value} value={p.value}>
                          {p.label}
                        </option>
                      ))}
                    </select>
                    <Icon name="caret" size={13} />
                  </label>
                </div>
              </div>

              <textarea
                className="composer-textarea"
                value={content}
                onChange={(e) => setContent(e.target.value)}
                placeholder={`What's on your mind, ${firstName(user.displayName)}?`}
                rows={showImageField ? 3 : 5}
                autoFocus
              />

              {showImageField && (
                <input
                  className="composer-image-input"
                  value={imageUrl}
                  onChange={(e) => setImageUrl(e.target.value)}
                  placeholder="Paste an image URL (https://…)"
                />
              )}

              {imageUrl.trim() && (
                <div className="post-media composer-preview">
                  <img src={imageUrl} alt="" />
                </div>
              )}

              <div className="composer-addrow">
                <span>Add to your post</span>
                <div className="composer-addbtns">
                  <button
                    type="button"
                    className={showImageField ? 'on' : ''}
                    onClick={() => setShowImageField((v) => !v)}
                    aria-label="Add photo"
                  >
                    <Icon name="photo" size={22} className="ic-photo" />
                  </button>
                  <button type="button" aria-label="Feeling">
                    <Icon name="feeling" size={22} className="ic-feeling" />
                  </button>
                  <button type="button" aria-label="Check in">
                    <Icon name="location" size={22} className="ic-location" />
                  </button>
                </div>
              </div>

              {error && <p className="form-error">{error}</p>}
            </div>
            <footer className="modal-foot">
              <button
                type="button"
                className="btn-primary block"
                onClick={submit}
                disabled={busy || (!content.trim() && !imageUrl.trim())}
              >
                {busy ? 'Posting…' : 'Post'}
              </button>
            </footer>
          </div>
        </div>
      )}
    </section>
  )
}
