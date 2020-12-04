import { html } from 'lit-html';
import { component } from 'haunted';

function FreHome() {
  return html`<article>
    <h1>Hello there!</h1>
    <a href="/profile">Profile</a>
  </article>`;
}

window.customElements.define('fre-home', component(FreHome));
