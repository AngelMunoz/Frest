import { html } from 'lit-html';
import { component } from 'haunted';

function FrePlaces(this: HTMLElement) {
  const submit = (event: Event) => {
    event.preventDefault();
  };
  return html`<form @submit="${submit}"></form> `;
}

window.customElements.define('fre-places', component(FrePlaces as any));
