import { html } from 'lit-html';
import { component } from 'haunted';

function FreProfile(this: HTMLElement) {
  const submit = (event: Event) => {
    event.preventDefault();
  };
  return html`<form @submit="${submit}"><h2>Form</h2></form> `;
}
window.customElements.define(
  'fre-profile',
  component<HTMLElement>(FreProfile as any),
);
