import { Camera } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import { useAuth } from '../contexts/AuthContext'
import { getApiErrorMessage } from '../api/client'
import {
  useChangePassword,
  useUpdateProfile,
  useUploadAvatar,
} from '../api/profile'
import { Field, fieldClassName, textareaClassName } from '../components/catalogue/formFields'
import { Avatar } from '../components/ios/Avatar'
import { FormSection } from '../components/ios/FormSection'
import { LargeTitleHeader } from '../components/ios/LargeTitleHeader'

export function ProfilePage() {
  const { user } = useAuth()
  const updateProfile = useUpdateProfile()
  const uploadAvatar = useUploadAvatar()
  const changePassword = useChangePassword()
  const fileRef = useRef<HTMLInputElement>(null)

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [phone, setPhone] = useState('')
  const [bio, setBio] = useState('')
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')

  useEffect(() => {
    if (!user) return
    setFirstName(user.firstName)
    setLastName(user.lastName)
    setPhone(user.phone ?? '')
    setBio(user.bio ?? '')
  }, [user])

  if (!user) {
    return null
  }

  async function handleSaveProfile() {
    try {
      await updateProfile.mutateAsync({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        phone: phone.trim() || undefined,
        bio: bio.trim() || undefined,
      })
      toast.success('Profile updated')
    } catch (error) {
      toast.error(getApiErrorMessage(error))
    }
  }

  async function handleAvatarChange(file: File) {
    try {
      await uploadAvatar.mutateAsync(file)
      toast.success('Avatar updated')
    } catch (error) {
      toast.error(getApiErrorMessage(error))
    }
  }

  async function handleChangePassword() {
    try {
      await changePassword.mutateAsync({
        currentPassword,
        newPassword,
        confirmPassword,
      })
      toast.success('Password changed')
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
    } catch (error) {
      toast.error(getApiErrorMessage(error))
    }
  }

  return (
    <div className="mx-auto max-w-5xl">
      <LargeTitleHeader title="Profile" subtitle="Your account settings" />

      <div className="mb-8 flex flex-col items-center gap-4">
        <div className="relative">
          <Avatar name={user.displayName} src={user.avatarUrl} size="lg" />
          <button
            type="button"
            onClick={() => fileRef.current?.click()}
            disabled={uploadAvatar.isPending}
            className="absolute -bottom-1 -right-1 rounded-full bg-accent p-2 text-white shadow-md hover:opacity-90 disabled:opacity-50"
            aria-label="Upload avatar"
          >
            <Camera className="h-4 w-4" />
          </button>
        </div>
        <input
          ref={fileRef}
          type="file"
          accept="image/png,image/jpeg,image/webp"
          className="sr-only"
          onChange={(event) => {
            const file = event.target.files?.[0]
            if (file) void handleAvatarChange(file)
            event.target.value = ''
          }}
        />
        <div className="text-center">
          <div className="text-lg font-semibold text-label">{user.displayName}</div>
          <div className="text-sm text-secondary-label">
            {user.jobRoleName} · {user.locationName}
          </div>
        </div>
      </div>

      <div className="grid gap-8 lg:grid-cols-2 lg:items-start">
      <FormSection title="Personal info">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="First name">
              <input
                value={firstName}
                onChange={(event) => setFirstName(event.target.value)}
                className={fieldClassName}
                autoComplete="given-name"
              />
            </Field>
            <Field label="Last name">
              <input
                value={lastName}
                onChange={(event) => setLastName(event.target.value)}
                className={fieldClassName}
                autoComplete="family-name"
              />
            </Field>
          </div>
          <Field label="Email">
            <input
              value={user.email}
              readOnly
              className={`${fieldClassName} cursor-not-allowed opacity-70`}
              title="Email cannot be changed here"
            />
          </Field>
          <Field label="Phone">
            <input
              type="tel"
              value={phone}
              onChange={(event) => setPhone(event.target.value)}
              className={fieldClassName}
              autoComplete="tel"
              placeholder="Optional"
            />
          </Field>
          <Field label="Bio">
            <textarea
              value={bio}
              onChange={(event) => setBio(event.target.value)}
              rows={3}
              className={textareaClassName}
              placeholder="A short note about your role or expertise"
            />
          </Field>
          <button
            type="button"
            disabled={!firstName.trim() || !lastName.trim() || updateProfile.isPending}
            onClick={() => void handleSaveProfile()}
            className="min-h-11 w-full rounded-xl bg-accent text-sm font-semibold text-white disabled:opacity-40"
          >
            {updateProfile.isPending ? 'Saving…' : 'Save profile'}
          </button>
      </FormSection>

      <FormSection title="Change password">
          <Field label="Current password">
            <input
              type="password"
              value={currentPassword}
              onChange={(event) => setCurrentPassword(event.target.value)}
              className={fieldClassName}
              autoComplete="current-password"
            />
          </Field>
          <Field label="New password">
            <input
              type="password"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              className={fieldClassName}
              autoComplete="new-password"
            />
          </Field>
          <Field label="Confirm new password">
            <input
              type="password"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              className={fieldClassName}
              autoComplete="new-password"
            />
          </Field>
          <button
            type="button"
            disabled={
              !currentPassword
              || !newPassword
              || newPassword !== confirmPassword
              || changePassword.isPending
            }
            onClick={() => void handleChangePassword()}
            className="min-h-11 w-full rounded-xl bg-elevated text-sm font-semibold text-label ring-1 ring-separator/50 disabled:opacity-40"
          >
            {changePassword.isPending ? 'Updating…' : 'Change password'}
          </button>
      </FormSection>
      </div>
    </div>
  )
}
