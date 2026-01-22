import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonComponent } from '../../components/shared/Button/button.component';
import { SmallIconComponent } from '../../components/shared/SmallIcon/small-icon.component';
import { HomeHeaderComponent } from './HomeHeader/home-header.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, ButtonComponent, SmallIconComponent, HomeHeaderComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent {
  faqItems = [
  // --- Catégorie : Cours et Fonctionnement ---
  {
    question: 'Comment réserver un cours ?',
    answer: 'Vous recherchez un professeur via notre moteur de recherche, vous échangez via la messagerie sécurisée, puis vous réservez directement depuis la plateforme.'
  },
  {
    question: 'Puis-je choisir entre cours à domicile et cours en visio ?',
    answer: 'Oui. Vous pouvez filtrer vos recherches pour afficher uniquement les professeurs qui donnent des cours à domicile, en visio ou les deux.'
  },
  {
    question: 'Comment sont fixés les tarifs ?',
    answer: 'Chaque professeur fixe librement ses tarifs. Ils s’affichent clairement sur son profil.'
  },

  // --- Catégorie : Paiements et Factures ---
  {
    question: 'Comment régler un cours ?',
    answer: 'Tous les paiements passent par la plateforme via carte bancaire pour garantir sécurité et traçabilité.'
  },
  {
    question: 'Quand suis-je débité ?',
    answer: 'Vous êtes débité uniquement après confirmation de la réservation.'
  },
  {
    question: 'Comment obtenir ma facture ?',
    answer: 'Une facture est générée automatiquement après chaque cours et disponible dans votre espace client.'
  },

  // --- Catégorie : Réduction d'impôt et Légal ---
  {
    question: 'Puis-je bénéficier d’une réduction d’impôt ?',
    answer: 'Oui, pour les cours à domicile, vous pouvez bénéficier d’un crédit ou d’une réduction d’impôt de 50 %. Une attestation vous est envoyée chaque année.'
  },
  {
    question: 'Les cours en visio donnent-ils droit à une réduction d’impôt ?',
    answer: 'Non, la réduction d’impôt s’applique uniquement aux cours effectués à domicile.'
  },

  // --- Catégorie : Sécurité et Fiabilité ---
  {
    question: 'Comment vérifiez-vous les professeurs ?',
    answer: 'Chaque professeur est validé manuellement : vérification d’identité, diplômes, expérience et références.'
  },
  {
    question: 'Puis-je laisser un avis ?',
    answer: 'Oui, chaque parent peut évaluer un professeur après un cours. Les avis sont publiés et visibles de tous.'
  },
];

  testimonials = [
    {
      name: 'Tom',
      role: 'Professeur de Mathématiques',
      content: 'En 3 mois j’ai trouvé 8 élèves réguliers. Paiements sécurisés, simplicité top.',
      stars: 5,
      img: 'assets/home/tom.png'
    },
    {
      name: 'Mme C.',
      role: 'Parent d\'élève (Paris)',
      content: 'Ma fille a repris confiance grâce à ces cours ! Un accompagnement humain et individualisé.',
      stars: 5,
      img: 'assets/home/mme_c.png'
    },
    {
      name: 'Karim',
      role: 'Professeur d\'Anglais',
      content: 'J’adore pouvoir donner mes cours en visio. Flexible et pratique pour mon emploi du temps.',
      stars: 4,
      img: 'assets/home/karim.png'
    },
    {
      name: 'Mme G.',
      role: 'Parent d\'élève (Corrèze)',
      content: 'On habite à la campagne, c’était la solution parfaite pour les maths en visio.',
      stars: 5,
      img: 'assets/home/mme_g.png'
    }
  ];
}