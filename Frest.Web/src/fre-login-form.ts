import { html } from 'lit-html';
import { useState, component, useEffect } from 'haunted';
import { Subject } from 'rxjs';
import { map, distinctUntilChanged, debounceTime } from 'rxjs/operators';

function FreLoginForm(this: HTMLElement) {
  const [state, setState] = useState<LoginPayload>({ email: '', password: '' });
  const email$ = new Subject<string>();
  const password$ = new Subject<string>();

  useEffect(() => {
    const subs = [
      email$
        .pipe(
          debounceTime(750),
          distinctUntilChanged(),
          map((email) => setState({ ...state, email })),
        )
        .subscribe(),
      password$
        .pipe(
          debounceTime(750),
          distinctUntilChanged(),
          map((password) => setState({ ...state, password })),
        )
        .subscribe(),
    ];
    return () => {
      subs.map((s) => s.unsubscribe());
    };
  });

  const submit = (event: Event) => {
    event.preventDefault();
    const evt = new CustomEvent<LoginPayload>('login', {
      bubbles: true,
      cancelable: true,
      composed: true,
      detail: { ...state },
    });
    this.dispatchEvent(evt);
  };
  return html`<form @submit="${submit}">
    <input
      type="email"
      value="${state.email}"
      @keyup="${(e: any) => email$.next(e.target.value)}"
    />
    <input
      type="password"
      value="${state.password}"
      @keyup="${(e: any) => password$.next(e.target.value)}"
    />
    <button type="submit" ?disabled="${!state.email || !state.password}">
      Login
    </button>
  </form> `;
}

window.customElements.define('fre-login-form', component(FreLoginForm as any));
