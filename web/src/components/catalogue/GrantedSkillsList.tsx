import { InsetGroupedList, Row } from '../ios/InsetGroupedList'
import type { GrantedSkill } from '../../api/catalogues'

export function GrantedSkillsList({ skills }: { skills: GrantedSkill[] }) {
  if (skills.length === 0) {
    return (
      <p className="text-sm text-secondary-label">No skills granted on completion.</p>
    )
  }

  return (
    <InsetGroupedList title="Granted skills">
      {skills.map((skill) => (
        <Row
          key={skill.skillId}
          label={skill.skillName}
          description={`Proficiency: ${skill.grantedProficiency}`}
        />
      ))}
    </InsetGroupedList>
  )
}
