import React, { Component } from 'react';

class QuestionAccordionCookiePolicy extends Component {
    render() {
        return (
            <div className="accordion">
            <button className="accordion__btn js-open-accordion">
                <span className="font-ico-chevron-down"></span> Koliko je sigurna moja privatnost? </button>
            <div className="accordion__content">
                <p>Podaci koje ste uneli u sistem neće biti javno dostupni i služe samo za realizovanje ideje ove aplikacije. Sa obradom podataka se možete detaljnije upoznati kroz <a class="footer__link" href="/privacy">Politiku privatnosti.</a> </p>
            </div>
        </div>
        );
    }
}

export default QuestionAccordionCookiePolicy;