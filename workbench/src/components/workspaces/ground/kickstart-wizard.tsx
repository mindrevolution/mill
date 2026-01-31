import { useState, useEffect } from 'react'
import { Loader2, ChevronRight, ChevronLeft, Sparkles, AlertTriangle } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { ScrollArea } from '@/components/ui/scroll-area'
import { TemplateCard } from './template-card'
import { api, type TemplateSummary, type KickstartRequest } from '@/lib/api'

type WizardStep = 'name' | 'archetype' | 'stack' | 'confirm'

interface KickstartWizardProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  showConfirmation?: boolean
  onComplete?: () => void
}

const STEPS: WizardStep[] = ['name', 'archetype', 'stack', 'confirm']

const STEP_TITLES: Record<WizardStep, string> = {
  name: 'Name Your Product',
  archetype: 'Choose an Archetype',
  stack: 'Select Your Stack',
  confirm: 'Review & Generate',
}

const STEP_DESCRIPTIONS: Record<WizardStep, string> = {
  name: 'Give your product a name and optional description.',
  archetype: 'Select the product archetype that best matches your vision.',
  stack: 'Choose the technology stack for your project.',
  confirm: 'Review your selections before generating ground files.',
}

export function KickstartWizard({
  open,
  onOpenChange,
  showConfirmation = false,
  onComplete,
}: KickstartWizardProps) {
  const [step, setStep] = useState<WizardStep>(showConfirmation ? 'confirm' : 'name')
  const [confirmedRerun, setConfirmedRerun] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Form state
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [archetypeId, setArchetypeId] = useState('')
  const [stackId, setStackId] = useState('')

  // Template data
  const [archetypes, setArchetypes] = useState<TemplateSummary[]>([])
  const [stacks, setStacks] = useState<TemplateSummary[]>([])
  const [loadingTemplates, setLoadingTemplates] = useState(true)

  // Load templates
  useEffect(() => {
    if (open) {
      setLoadingTemplates(true)
      Promise.all([api.ground.archetypes(), api.ground.stacks()])
        .then(([archetypeData, stackData]) => {
          setArchetypes(archetypeData)
          setStacks(stackData)
        })
        .catch((err) => {
          console.error('Failed to load templates:', err)
          setError('Failed to load templates')
        })
        .finally(() => setLoadingTemplates(false))
    }
  }, [open])

  // Reset state when dialog opens
  useEffect(() => {
    if (open) {
      setStep(showConfirmation && !confirmedRerun ? 'confirm' : 'name')
      setError(null)
      if (!showConfirmation) {
        setName('')
        setDescription('')
        setArchetypeId('')
        setStackId('')
      }
    }
  }, [open, showConfirmation, confirmedRerun])

  const currentStepIndex = STEPS.indexOf(step)
  const isFirstStep = currentStepIndex === 0
  const isLastStep = currentStepIndex === STEPS.length - 1

  const canProceed = () => {
    switch (step) {
      case 'name':
        return name.trim().length > 0
      case 'archetype':
        return archetypeId.length > 0
      case 'stack':
        return stackId.length > 0
      case 'confirm':
        return name.trim().length > 0 && archetypeId.length > 0 && stackId.length > 0
      default:
        return false
    }
  }

  const handleNext = () => {
    if (isLastStep) {
      handleGenerate()
    } else {
      setStep(STEPS[currentStepIndex + 1])
    }
  }

  const handleBack = () => {
    if (!isFirstStep) {
      setStep(STEPS[currentStepIndex - 1])
    }
  }

  const handleGenerate = async () => {
    setIsLoading(true)
    setError(null)

    try {
      const request: KickstartRequest = {
        name: name.trim(),
        description: description.trim() || undefined,
        archetypeId,
        stackId,
      }

      await api.ground.kickstart(request)
      onOpenChange(false)
      onComplete?.()
    } catch (err) {
      console.error('Kickstart failed:', err)
      setError(err instanceof Error ? err.message : 'Failed to generate ground files')
    } finally {
      setIsLoading(false)
    }
  }

  const handleRerunConfirm = () => {
    setConfirmedRerun(true)
    setStep('name')
  }

  const selectedArchetype = archetypes.find((a) => a.id === archetypeId)
  const selectedStack = stacks.find((s) => s.id === stackId)

  // Show rerun confirmation if ground already exists
  if (showConfirmation && !confirmedRerun && step === 'confirm') {
    return (
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-yellow-500" />
              Re-run Kickstart?
            </DialogTitle>
            <DialogDescription>
              Ground files already exist in this project. Running kickstart again will overwrite
              the existing product, persona, and standards files.
            </DialogDescription>
          </DialogHeader>
          <div className="flex justify-end gap-2 pt-4">
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button onClick={handleRerunConfirm}>Continue</Button>
          </div>
        </DialogContent>
      </Dialog>
    )
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl max-h-[85vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>{STEP_TITLES[step]}</DialogTitle>
          <DialogDescription>{STEP_DESCRIPTIONS[step]}</DialogDescription>
        </DialogHeader>

        {/* Progress indicator */}
        <div className="flex gap-1 mb-2">
          {STEPS.map((s, i) => (
            <div
              key={s}
              className={`h-1 flex-1 rounded-full transition-colors ${
                i <= currentStepIndex ? 'bg-[#ffcc00]' : 'bg-muted'
              }`}
            />
          ))}
        </div>

        {/* Step content */}
        <div className="flex-1 min-h-0 py-4">
          {loadingTemplates ? (
            <div className="flex items-center justify-center h-full">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : (
            <>
              {/* Name step */}
              {step === 'name' && (
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="name">Product Name</Label>
                    <Input
                      id="name"
                      placeholder="e.g., Acme Dashboard"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      autoFocus
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="description">Description (optional)</Label>
                    <Textarea
                      id="description"
                      placeholder="A brief description of what your product does..."
                      value={description}
                      onChange={(e) => setDescription(e.target.value)}
                      rows={3}
                    />
                  </div>
                </div>
              )}

              {/* Archetype step */}
              {step === 'archetype' && (
                <ScrollArea className="h-[340px] pr-4">
                  <div className="grid grid-cols-2 gap-3">
                    {archetypes.map((archetype) => (
                      <TemplateCard
                        key={archetype.id}
                        template={archetype}
                        selected={archetypeId === archetype.id}
                        onSelect={setArchetypeId}
                      />
                    ))}
                  </div>
                </ScrollArea>
              )}

              {/* Stack step */}
              {step === 'stack' && (
                <ScrollArea className="h-[340px] pr-4">
                  <div className="grid grid-cols-2 gap-3">
                    {stacks.map((stack) => (
                      <TemplateCard
                        key={stack.id}
                        template={stack}
                        selected={stackId === stack.id}
                        onSelect={setStackId}
                      />
                    ))}
                  </div>
                </ScrollArea>
              )}

              {/* Confirm step */}
              {step === 'confirm' && (
                <div className="space-y-4">
                  <div className="rounded-lg border border-border/50 bg-muted/30 p-4 space-y-3">
                    <div>
                      <span className="text-xs text-muted-foreground">Product</span>
                      <p className="font-medium">{name}</p>
                      {description && (
                        <p className="text-sm text-muted-foreground mt-1">{description}</p>
                      )}
                    </div>
                    <div className="border-t border-border/50 pt-3">
                      <span className="text-xs text-muted-foreground">Archetype</span>
                      <p className="font-medium">{selectedArchetype?.label}</p>
                      <p className="text-sm text-muted-foreground">{selectedArchetype?.summary}</p>
                    </div>
                    <div className="border-t border-border/50 pt-3">
                      <span className="text-xs text-muted-foreground">Stack</span>
                      <p className="font-medium">{selectedStack?.label}</p>
                      <p className="text-sm text-muted-foreground">{selectedStack?.summary}</p>
                    </div>
                  </div>

                  <div className="text-sm text-muted-foreground">
                    This will create initial ground files:
                    <ul className="list-disc list-inside mt-1 space-y-0.5">
                      <li>product.md — product definition</li>
                      <li>personas/primary-user.md — target user profile</li>
                      <li>standards/tech-stack.md — technology choices</li>
                      <li>standards/quality-bars.md — quality requirements</li>
                    </ul>
                  </div>

                  {error && (
                    <div className="rounded-lg border border-red-500/50 bg-red-500/10 p-3 text-sm text-red-400">
                      {error}
                    </div>
                  )}
                </div>
              )}
            </>
          )}
        </div>

        {/* Footer */}
        <div className="flex justify-between pt-2 border-t">
          <Button
            variant="ghost"
            onClick={handleBack}
            disabled={isFirstStep || isLoading}
          >
            <ChevronLeft className="h-4 w-4 mr-1" />
            Back
          </Button>
          <Button onClick={handleNext} disabled={!canProceed() || isLoading}>
            {isLoading ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Generating...
              </>
            ) : isLastStep ? (
              <>
                <Sparkles className="h-4 w-4 mr-2" />
                Generate
              </>
            ) : (
              <>
                Next
                <ChevronRight className="h-4 w-4 ml-1" />
              </>
            )}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
