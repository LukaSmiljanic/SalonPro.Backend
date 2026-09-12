import React, { useEffect, useId, useRef, useState } from 'react';
import { Calendar } from 'lucide-react';
import { displayDateToIso, isoDateToDisplay } from '../lib/dateUtils';

interface DateInputProps {
  label?: string;
  value: string;
  onChange: (isoDate: string) => void;
  min?: string;
  max?: string;
  required?: boolean;
  disabled?: boolean;
  className?: string;
  id?: string;
}

export const DateInput: React.FC<DateInputProps> = ({
  label,
  value,
  onChange,
  min,
  max,
  required,
  disabled,
  className = '',
  id,
}) => {
  const autoId = useId();
  const inputId = id ?? autoId;
  const hiddenRef = useRef<HTMLInputElement>(null);
  const [text, setText] = useState(() => isoDateToDisplay(value));

  useEffect(() => {
    setText(isoDateToDisplay(value));
  }, [value]);

  const commitText = (raw: string) => {
    const iso = displayDateToIso(raw);
    if (iso) {
      if (min && iso < min) {
        setText(isoDateToDisplay(value));
        return;
      }
      if (max && iso > max) {
        setText(isoDateToDisplay(value));
        return;
      }
      onChange(iso);
      setText(isoDateToDisplay(iso));
      return;
    }
    setText(isoDateToDisplay(value));
  };

  const openPicker = () => {
    if (disabled) return;
    const el = hiddenRef.current;
    if (!el) return;
    try {
      el.showPicker();
    } catch {
      el.focus();
      el.click();
    }
  };

  return (
    <div className={`flex flex-col gap-1 ${className}`}>
      {label && (
        <label htmlFor={inputId} className="text-sm font-medium text-text">
          {label}
        </label>
      )}
      <div className="relative flex items-center">
        <input
          id={inputId}
          type="text"
          inputMode="numeric"
          autoComplete="off"
          placeholder="dd.mm.gggg"
          value={text}
          disabled={disabled}
          required={required}
          lang="sr"
          onChange={e => setText(e.target.value)}
          onBlur={e => commitText(e.target.value)}
          onKeyDown={e => {
            if (e.key === 'Enter') {
              e.preventDefault();
              commitText((e.target as HTMLInputElement).value);
            }
          }}
          className="w-full h-11 md:h-9 bg-surface border border-border rounded-lg md:rounded-md pl-3 pr-10
            text-base md:text-sm text-text placeholder:text-text-faint
            transition-interactive
            focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary
            disabled:opacity-50 disabled:cursor-not-allowed"
        />
        <button
          type="button"
          tabIndex={-1}
          disabled={disabled}
          onClick={openPicker}
          className="absolute right-2 p-1 rounded-md text-text-muted hover:text-text hover:bg-surface-2
            transition-colors disabled:opacity-50 disabled:pointer-events-none"
          aria-label="Otvori kalendar"
        >
          <Calendar size={16} />
        </button>
        <input
          ref={hiddenRef}
          type="date"
          lang="sr"
          tabIndex={-1}
          aria-hidden
          className="sr-only"
          value={value}
          min={min}
          max={max}
          disabled={disabled}
          onChange={e => onChange(e.target.value)}
        />
      </div>
    </div>
  );
};
