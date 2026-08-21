import { Plus, Trash2 } from 'lucide-react'
import type { ProficiencyLevel, Skill } from '../../api/catalogues'
import { Field, fieldClassName } from './formFields'

export interface SkillGrantInput {
  skillId: number
  grantedProficiency: ProficiencyLevel
}

const PROFICIENCY_LEVELS: ProficiencyLevel[] = ['Beginner', 'Intermediate', 'Advanced', 'Expert']

export function GrantedSkillsEditor({
  grants,
  skills,
  onChange,
}: {
  grants: SkillGrantInput[]
  skills: Skill[]
  onChange: (grants: SkillGrantInput[]) => void
}) {
  const activeSkills = skills.filter((skill) => skill.isActive)

  function addGrant() {
    const firstSkill = activeSkills.find((skill) => !grants.some((g) => g.skillId === skill.id))
    if (!firstSkill) return
    onChange([...grants, { skillId: firstSkill.id, grantedProficiency: 'Beginner' }])
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-sm font-medium text-label">Granted skills</p>
        <button
          type="button"
          onClick={addGrant}
          disabled={activeSkills.length === 0 || grants.length >= activeSkills.length}
          className="inline-flex items-center gap-1 rounded-lg bg-accent-muted px-2.5 py-1 text-xs font-semibold text-accent disabled:opacity-40"
        >
          <Plus className="h-3.5 w-3.5" />
          Add skill
        </button>
      </div>
      {grants.length === 0 ? (
        <p className="text-sm text-secondary-label">
          No skills granted on completion. Add skills that employees receive when awarded or completing this item.
        </p>
      ) : (
        grants.map((grant, index) => (
          <div key={index} className="flex items-end gap-2">
            <Field label="Skill" className="min-w-0 flex-1">
              <select
                value={grant.skillId}
                onChange={(event) => {
                  const next = [...grants]
                  next[index] = { ...grant, skillId: Number(event.target.value) }
                  onChange(next)
                }}
                className={fieldClassName}
              >
                {activeSkills.map((skill) => (
                  <option key={skill.id} value={skill.id}>
                    {skill.name}
                  </option>
                ))}
              </select>
            </Field>
            <Field label="Proficiency" className="w-36">
              <select
                value={grant.grantedProficiency}
                onChange={(event) => {
                  const next = [...grants]
                  next[index] = {
                    ...grant,
                    grantedProficiency: event.target.value as ProficiencyLevel,
                  }
                  onChange(next)
                }}
                className={fieldClassName}
              >
                {PROFICIENCY_LEVELS.map((level) => (
                  <option key={level} value={level}>
                    {level}
                  </option>
                ))}
              </select>
            </Field>
            <button
              type="button"
              aria-label="Remove skill grant"
              onClick={() => onChange(grants.filter((_, i) => i !== index))}
              className="mb-1 rounded-lg p-2 text-[var(--color-status-danger)] hover:bg-elevated"
            >
              <Trash2 className="h-4 w-4" />
            </button>
          </div>
        ))
      )}
    </div>
  )
}
